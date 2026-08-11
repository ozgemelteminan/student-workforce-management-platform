using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Infrastructure.Storage;
using ExportRequest = StudentWorkforceManagement.Domain.Entities.ExportRequest;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.DataExport;

public sealed class DataExportJob(
    ApplicationDbContext dbContext,
    IFileStorage storage,
    IOptions<DataExportOptions> options,
    ILogger<DataExportJob> logger)
{
    [AutomaticRetry(Attempts = 1)]
    public async Task<int> RunAsync(Guid exportRequestId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (!await ClaimQueuedExportAsync(exportRequestId, now, cancellationToken))
        {
            logger.LogInformation("Export request {ExportRequestId} was not queued; skipping generation.", exportRequestId);
            return 0;
        }

        var request = await dbContext.ExportRequests.SingleAsync(entity => entity.Id == exportRequestId, cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"swm-export-{exportRequestId:N}-{Guid.NewGuid():N}.tmp");

        try
        {
            var rows = await BuildRowsAsync(request, cancellationToken);
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 64, useAsync: true))
            {
                await WriteArtifactAsync(output, request, rows, cancellationToken);
            }

            var fileInfo = new FileInfo(tempPath);
            var fileName = BuildFileName(request);
            var mimeType = GetMimeType(request.Format);
            var fileExtension = Path.GetExtension(fileName);
            var storageKey = StorageKeyFactory.Create(new UploadTargetRequest(fileName, fileInfo.Length, mimeType, fileExtension, $"exports-{request.Id:N}", RequiresMultipartUpload: false));
            var contentHash = await ComputeSha256Async(tempPath, cancellationToken);

            await using (var input = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64, useAsync: true))
            {
                await storage.SaveAsync(storageKey, input, mimeType, cancellationToken);
            }

            request.Status = ExportStatus.COMPLETED;
            request.CompletedAt = DateTimeOffset.UtcNow;
            request.ExpiresAt = request.CompletedAt.Value.AddHours(options.Value.ArtifactExpirationHours);
            request.FailureReason = null;
            request.ArtifactStorageKey = storageKey;
            request.ArtifactFileName = fileName;
            request.ArtifactFileSize = fileInfo.Length;
            request.ArtifactMimeType = mimeType;
            request.ArtifactContentHash = contentHash;
            await dbContext.SaveChangesAsync(cancellationToken);
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export request {ExportRequestId} failed during generation.", exportRequestId);
            request.Status = ExportStatus.FAILED;
            request.FailedAt = DateTimeOffset.UtcNow;
            request.FailureReason = SanitizeFailure(ex.Message);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            return 0;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private async Task<bool> ClaimQueuedExportAsync(Guid exportRequestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory" && dbContext.Database.IsRelational())
        {
            var claimed = await dbContext.ExportRequests
                .Where(entity => entity.Id == exportRequestId && entity.Status == ExportStatus.QUEUED)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(entity => entity.Status, ExportStatus.PROCESSING)
                    .SetProperty(entity => entity.ProcessingStartedAt, now)
                    .SetProperty(entity => entity.UpdatedAt, now), cancellationToken);
            return claimed == 1;
        }

        var export = await dbContext.ExportRequests.SingleOrDefaultAsync(entity => entity.Id == exportRequestId, cancellationToken);
        if (export is null || export.Status != ExportStatus.QUEUED)
        {
            return false;
        }

        export.Status = ExportStatus.PROCESSING;
        export.ProcessingStartedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<string[]>> BuildRowsAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        return request.ExportType switch
        {
            ExportType.Tasks => await BuildTaskRowsAsync(cancellationToken),
            ExportType.Workload => await BuildWorkloadRowsAsync(cancellationToken),
            ExportType.Students => await BuildStudentRowsAsync(cancellationToken),
            ExportType.Semester => await BuildSemesterRowsAsync(request.ScopeId, cancellationToken),
            ExportType.PersonalData => await BuildPersonalDataRowsAsync(request.RequestingUserId, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported export type.")
        };
    }

    private async Task<IReadOnlyList<string[]>> BuildTaskRowsAsync(CancellationToken cancellationToken)
    {
        var rows = new List<string[]> { new[] { "Id", "Title", "Status", "Priority", "Difficulty", "AssignedStudentId", "Deadline" } };
        rows.AddRange(await dbContext.Tasks.AsNoTracking()
            .OrderBy(task => task.Deadline)
            .Select(task => new[]
            {
                task.Id.ToString(),
                task.Title,
                task.Status.ToString(),
                task.Priority.ToString(),
                task.Difficulty.ToString(),
                task.AssignedStudentId.HasValue ? task.AssignedStudentId.Value.ToString() : string.Empty,
                task.Deadline.ToString("O", CultureInfo.InvariantCulture)
            })
            .ToListAsync(cancellationToken));
        return rows;
    }

    private async Task<IReadOnlyList<string[]>> BuildWorkloadRowsAsync(CancellationToken cancellationToken)
    {
        var tasks = dbContext.Tasks.AsNoTracking()
            .Where(task => task.AssignedStudentId.HasValue && task.Status != TaskStatus.COMPLETED && task.Status != TaskStatus.CANCELLED);
        var rows = new List<string[]> { new[] { "StudentId", "Name", "ActiveTaskCount", "EstimatedMinutes" } };
        rows.AddRange(await dbContext.Students.AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .Select(student => new[]
            {
                student.Id.ToString(),
                student.FirstName + " " + student.LastName,
                tasks.Count(task => task.AssignedStudentId == student.Id).ToString(CultureInfo.InvariantCulture),
                tasks.Where(task => task.AssignedStudentId == student.Id).Sum(task => task.EstimatedDurationMinutes).ToString(CultureInfo.InvariantCulture)
            })
            .ToListAsync(cancellationToken));
        return rows;
    }

    private async Task<IReadOnlyList<string[]>> BuildStudentRowsAsync(CancellationToken cancellationToken)
    {
        var rows = new List<string[]> { new[] { "Id", "FirstName", "LastName", "Email", "Department", "IsActive" } };
        rows.AddRange(await dbContext.Students.AsNoTracking()
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .Select(student => new[]
            {
                student.Id.ToString(),
                student.FirstName,
                student.LastName,
                student.Email,
                student.Department,
                student.IsActive.ToString(CultureInfo.InvariantCulture)
            })
            .ToListAsync(cancellationToken));
        return rows;
    }

    private async Task<IReadOnlyList<string[]>> BuildSemesterRowsAsync(Guid? semesterId, CancellationToken cancellationToken)
    {
        var query = dbContext.Semesters.AsNoTracking();
        if (semesterId.HasValue)
        {
            query = query.Where(semester => semester.Id == semesterId.Value);
        }

        var rows = new List<string[]> { new[] { "Id", "Name", "StartDate", "EndDate", "Status" } };
        rows.AddRange(await query.OrderByDescending(semester => semester.StartDate)
            .Select(semester => new[]
            {
                semester.Id.ToString(),
                semester.Name,
                semester.StartDate.ToString("O", CultureInfo.InvariantCulture),
                semester.EndDate.ToString("O", CultureInfo.InvariantCulture),
                semester.Status.ToString()
            })
            .ToListAsync(cancellationToken));
        return rows;
    }

    private async Task<IReadOnlyList<string[]>> BuildPersonalDataRowsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Include(entity => entity.Student)
            .SingleAsync(entity => entity.Id == userId, cancellationToken);
        var rows = new List<string[]>
        {
            new[] { "Section", "Field", "Value" },
            new[] { "User", "Id", user.Id.ToString() },
            new[] { "User", "Email", user.Email },
            new[] { "User", "DisplayName", user.DisplayName },
            new[] { "User", "IsActive", user.IsActive.ToString(CultureInfo.InvariantCulture) }
        };

        if (user.Student is not null)
        {
            rows.Add(new[] { "Student", "Id", user.Student.Id.ToString() });
            rows.Add(new[] { "Student", "FirstName", user.Student.FirstName });
            rows.Add(new[] { "Student", "LastName", user.Student.LastName });
            rows.Add(new[] { "Student", "Department", user.Student.Department });

            var assignedTasks = await dbContext.Tasks.AsNoTracking()
                .Where(task => task.AssignedStudentId == user.Student.Id)
                .OrderBy(task => task.Deadline)
                .Select(task => new { task.Id, task.Title, task.Status, task.Deadline })
                .ToListAsync(cancellationToken);
            foreach (var task in assignedTasks)
            {
                rows.Add(new[] { "AssignedTask", task.Id.ToString(), $"{task.Title} | {task.Status} | {task.Deadline:O}" });
            }
        }

        return rows;
    }

    private static async Task WriteArtifactAsync(Stream output, ExportRequest request, IReadOnlyList<string[]> rows, CancellationToken cancellationToken)
    {
        switch (request.Format)
        {
            case ExportFormat.Csv:
                await WriteCsvAsync(output, rows, cancellationToken);
                break;
            case ExportFormat.Xlsx:
                WriteXlsx(output, rows);
                break;
            case ExportFormat.Pdf:
                await WritePdfAsync(output, request, rows, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("Unsupported export format.");
        }
    }

    private static async Task WriteCsvAsync(Stream output, IReadOnlyList<string[]> rows, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
        foreach (var row in rows)
        {
            await writer.WriteLineAsync(string.Join(",", row.Select(EscapeCsv)).AsMemory(), cancellationToken);
        }
    }

    private static void WriteXlsx(Stream output, IReadOnlyList<string[]> rows)
    {
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        AddEntry(archive, "[Content_Types].xml", """
            <?xml version="1.0" encoding="UTF-8"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
              <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
            </Types>
            """);
        AddEntry(archive, "_rels/.rels", """
            <?xml version="1.0" encoding="UTF-8"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
            </Relationships>
            """);
        AddEntry(archive, "xl/_rels/workbook.xml.rels", """
            <?xml version="1.0" encoding="UTF-8"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
            </Relationships>
            """);
        AddEntry(archive, "xl/workbook.xml", """
            <?xml version="1.0" encoding="UTF-8"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets><sheet name="Export" sheetId="1" r:id="rId1"/></sheets>
            </workbook>
            """);

        var sheet = new StringBuilder("""<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
        for (var r = 0; r < rows.Count; r++)
        {
            sheet.Append(CultureInfo.InvariantCulture, $"<row r=\"{r + 1}\">");
            for (var c = 0; c < rows[r].Length; c++)
            {
                var reference = $"{ColumnName(c)}{r + 1}";
                sheet.Append(CultureInfo.InvariantCulture, $"<c r=\"{reference}\" t=\"inlineStr\"><is><t>{WebUtility.HtmlEncode(rows[r][c])}</t></is></c>");
            }
            sheet.Append("</row>");
        }
        sheet.Append("</sheetData></worksheet>");
        AddEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
    }

    private static async Task WritePdfAsync(Stream output, ExportRequest request, IReadOnlyList<string[]> rows, CancellationToken cancellationToken)
    {
        var lines = rows.Take(40).Select(row => string.Join(" | ", row).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")).ToArray();
        var content = new StringBuilder("BT /F1 10 Tf 40 760 Td ");
        content.Append(CultureInfo.InvariantCulture, $"({request.ExportType} export) Tj ");
        foreach (var line in lines)
        {
            content.Append("0 -14 Td (");
            content.Append(ToPdfAscii(line));
            content.Append(") Tj ");
        }
        content.Append("ET");

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}\nendstream"
        };

        await WriteAsciiAsync(output, "%PDF-1.4\n", cancellationToken);
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            await WriteAsciiAsync(output, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n", cancellationToken);
        }

        var xref = output.Position;
        await WriteAsciiAsync(output, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n", cancellationToken);
        foreach (var offset in offsets.Skip(1))
        {
            await WriteAsciiAsync(output, $"{offset:0000000000} 00000 n \n", cancellationToken);
        }
        await WriteAsciiAsync(output, $"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n", cancellationToken);
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64, useAsync: true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task WriteAsciiAsync(Stream output, string value, CancellationToken cancellationToken)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        await output.WriteAsync(bytes, cancellationToken);
    }

    private static string BuildFileName(ExportRequest request)
    {
        var extension = request.Format switch
        {
            ExportFormat.Csv => "csv",
            ExportFormat.Xlsx => "xlsx",
            ExportFormat.Pdf => "pdf",
            _ => "dat"
        };
        return $"{request.ExportType.ToString().ToLowerInvariant()}-{request.Id:N}.{extension}";
    }

    private static string GetMimeType(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Csv => "text/csv",
            ExportFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFormat.Pdf => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    private static string EscapeCsv(string value)
    {
        return value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }

    private static string ColumnName(int index)
    {
        var value = string.Empty;
        index++;
        while (index > 0)
        {
            var modulo = (index - 1) % 26;
            value = (char)('A' + modulo) + value;
            index = (index - modulo) / 26;
        }
        return value;
    }

    private static string ToPdfAscii(string value)
    {
        var sanitized = new string(value.Select(ch => ch is >= ' ' and <= '~' ? ch : '?').ToArray());
        return sanitized.Length <= 100 ? sanitized : sanitized[..100];
    }

    private static string SanitizeFailure(string message)
    {
        var sanitized = message.ReplaceLineEndings(" ").Replace('\t', ' ');
        return sanitized.Length <= 2000 ? sanitized : sanitized[..2000];
    }
}
