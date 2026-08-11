using System.ComponentModel.DataAnnotations;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Application.Files.Services;

public interface IUploadFilePolicy
{
    ValidatedUploadFile ValidatePendingUpload(string fileName, long fileSizeBytes, string declaredMimeType, string fileExtension);
    void ValidateStoredObject(FileMetadata pendingFile, StoredFileMetadata storedMetadata);
}

public sealed record ValidatedUploadFile(string FileName, long FileSizeBytes, string MimeType, string FileExtension);

public sealed class UploadFilePolicyOptions
{
    public const string SectionName = "UploadFilePolicy";
    public const long OneGigabyteInBytes = 1_073_741_824;

    [Range(1, OneGigabyteInBytes)]
    public long MaxFileSizeBytes { get; init; } = OneGigabyteInBytes;

    public Dictionary<string, string[]> AllowedMimeTypesByExtension { get; init; } = DefaultAllowedMimeTypesByExtension();

    public static Dictionary<string, string[]> DefaultAllowedMimeTypesByExtension()
    {
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".doc"] = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xls"] = ["application/vnd.ms-excel"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".ppt"] = ["application/vnd.ms-powerpoint"],
            [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
            [".odt"] = ["application/vnd.oasis.opendocument.text"],
            [".ods"] = ["application/vnd.oasis.opendocument.spreadsheet"],
            [".odp"] = ["application/vnd.oasis.opendocument.presentation"],
            [".pdf"] = ["application/pdf"],
            [".txt"] = ["text/plain"],
            [".csv"] = ["text/csv", "application/csv"],
            [".json"] = ["application/json", "text/json"],
            [".md"] = ["text/markdown", "text/plain"],
            [".png"] = ["image/png"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".webp"] = ["image/webp"],
            [".zip"] = ["application/zip", "application/x-zip-compressed", "multipart/x-zip"]
        };
    }
}

public sealed class UploadFilePolicy(Microsoft.Extensions.Options.IOptions<UploadFilePolicyOptions> options) : IUploadFilePolicy
{
    public ValidatedUploadFile ValidatePendingUpload(string fileName, long fileSizeBytes, string declaredMimeType, string fileExtension)
    {
        var trimmedName = fileName.Trim();
        ValidateSize(fileSizeBytes, options.Value.MaxFileSizeBytes);
        var extension = NormalizeExtension(fileExtension);
        var fileNameExtension = Path.GetExtension(trimmedName);
        if (!string.IsNullOrWhiteSpace(fileNameExtension) && !string.Equals(extension, NormalizeExtension(fileNameExtension), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("File extension does not match the original file name.");
        }

        var mimeType = NormalizeMimeType(declaredMimeType);
        EnsureAllowed(extension, mimeType);
        return new ValidatedUploadFile(trimmedName, fileSizeBytes, mimeType, extension);
    }

    public void ValidateStoredObject(FileMetadata pendingFile, StoredFileMetadata storedMetadata)
    {
        ValidateSize(storedMetadata.FileSizeBytes, options.Value.MaxFileSizeBytes);
        var extension = NormalizeExtension(pendingFile.FileExtension);
        if (!string.Equals(storedMetadata.StorageKey, pendingFile.StorageKey, StringComparison.Ordinal))
        {
            throw new ConflictException("Uploaded object storage key does not match the pending file metadata.");
        }

        var storageKeyExtension = Path.GetExtension(storedMetadata.StorageKey);
        if (!string.IsNullOrWhiteSpace(storageKeyExtension) && !string.Equals(extension, NormalizeExtension(storageKeyExtension), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Uploaded object storage key extension does not match the pending file metadata.");
        }

        var pendingMimeType = NormalizeMimeType(pendingFile.MimeType);
        var storedMimeType = NormalizeMimeType(storedMetadata.MimeType);
        EnsureAllowed(extension, pendingMimeType);
        EnsureAllowed(extension, storedMimeType);
        if (storedMetadata.FileSizeBytes != pendingFile.FileSize || !string.Equals(storedMimeType, pendingMimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Uploaded object metadata does not match the pending file metadata.");
        }
    }

    public static bool ValidateOptions(UploadFilePolicyOptions policyOptions)
    {
        return policyOptions.MaxFileSizeBytes is > 0 and <= UploadFilePolicyOptions.OneGigabyteInBytes
            && policyOptions.AllowedMimeTypesByExtension.Count > 0
            && policyOptions.AllowedMimeTypesByExtension.All(pair => IsNormalizedSafeExtension(pair.Key) && pair.Value.Length > 0 && pair.Value.All(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static void ValidateSize(long size, long maxSize)
    {
        if (size <= 0 || size > maxSize)
        {
            throw new ConflictException("File size must be between 1 byte and 1 GB.");
        }
    }

    private string NormalizeExtension(string extension)
    {
        var normalized = extension.Trim().StartsWith('.') ? extension.Trim().ToLowerInvariant() : $".{extension.Trim().ToLowerInvariant()}";
        if (!IsNormalizedSafeExtension(normalized) || !options.Value.AllowedMimeTypesByExtension.ContainsKey(normalized))
        {
            throw new ConflictException("File extension is not allowed.");
        }

        return normalized;
    }

    private void EnsureAllowed(string extension, string mimeType)
    {
        if (!options.Value.AllowedMimeTypesByExtension.TryGetValue(extension, out var allowedMimeTypes)
            || !allowedMimeTypes.Select(NormalizeMimeType).Contains(mimeType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ConflictException("File MIME type is not allowed for the file extension.");
        }
    }

    private static string NormalizeMimeType(string mimeType)
    {
        var normalized = mimeType.Trim().ToLowerInvariant();
        var parameterIndex = normalized.IndexOf(';', StringComparison.Ordinal);
        return parameterIndex >= 0 ? normalized[..parameterIndex].Trim() : normalized;
    }

    private static bool IsNormalizedSafeExtension(string extension)
    {
        return extension.StartsWith(".", StringComparison.Ordinal)
            && extension.Length is > 1 and <= 20
            && extension.Skip(1).All(char.IsLetterOrDigit);
    }
}
