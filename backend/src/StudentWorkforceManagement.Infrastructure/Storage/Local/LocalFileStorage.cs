using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Storage;

namespace StudentWorkforceManagement.Infrastructure.Storage.Local;

public sealed class LocalFileStorage(IOptions<StorageOptions> options, IDataProtectionProvider dataProtectionProvider) : IFileStorage
{
    private readonly StorageOptions _options = options.Value;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("StudentWorkforceManagement.LocalFileStorage.SignedUrls.v1");

    public Task<SignedUploadTarget> CreateUploadTargetAsync(UploadTargetRequest request, CancellationToken cancellationToken = default)
    {
        var storageKey = StorageKeyFactory.Create(request);
        EnsureSafeStorageKey(storageKey);
        var uploadId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.SignedUrlLifetimeMinutes);
        var token = CreateToken("PUT", storageKey, expiresAt);
        var uri = new Uri($"/api/v1/storage/local/uploads/{uploadId:N}?token={Uri.EscapeDataString(token)}", UriKind.Relative);
        return Task.FromResult(new SignedUploadTarget(uploadId, storageKey, uri, expiresAt, request.RequiresMultipartUpload, "PUT", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = request.MimeType
        }));
    }

    public Task<SignedDownloadTarget> CreateDownloadTargetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        EnsureSafeStorageKey(storageKey);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.SignedUrlLifetimeMinutes);
        var token = CreateToken("GET", storageKey, expiresAt);
        var uri = new Uri($"/api/v1/storage/local/downloads?token={Uri.EscapeDataString(token)}", UriKind.Relative);
        return Task.FromResult(new SignedDownloadTarget(uri, expiresAt));
    }

    public Task<StoredFileMetadata?> GetMetadataAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            return Task.FromResult<StoredFileMetadata?>(null);
        }

        var info = new FileInfo(path);
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".odt" => "application/vnd.oasis.opendocument.text",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".odp" => "application/vnd.oasis.opendocument.presentation",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".md" => "text/markdown",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
        return Task.FromResult<StoredFileMetadata?>(new StoredFileMetadata(storageKey, info.Length, mimeType, null));
    }

    public async Task SaveAsync(string storageKey, Stream content, string mimeType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _options.LocalRootPath);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 64, useAsync: true);
        await content.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        File.Delete(path);
        return Task.CompletedTask;
    }

    public string ResolvePath(string storageKey)
    {
        EnsureSafeStorageKey(storageKey);
        var root = Path.GetFullPath(_options.LocalRootPath);
        var path = Path.GetFullPath(Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Storage key resolves outside the configured local storage root.");
        }
        return path;
    }

    public bool TryValidateToken(string token, string method, out string storageKey)
    {
        storageKey = string.Empty;
        try
        {
            var payload = _protector.Unprotect(token);
            var parts = payload.Split('|');
            if (parts.Length != 3 || !string.Equals(parts[0], method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixTime))
            {
                return false;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            EnsureSafeStorageKey(parts[1]);
            storageKey = parts[1];
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string CreateToken(string method, string storageKey, DateTimeOffset expiresAt)
    {
        EnsureSafeStorageKey(storageKey);
        return _protector.Protect(string.Join('|', method.ToUpperInvariant(), storageKey, expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
    }

    private static void EnsureSafeStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey) || storageKey.Contains("..", StringComparison.Ordinal) || storageKey.Contains('\u005c'))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }
    }
}
