using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Storage;

namespace StudentWorkforceManagement.Infrastructure.Storage.Local;

public sealed class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private readonly StorageOptions _options = options.Value;

    public Task<SignedUploadTarget> CreateUploadTargetAsync(UploadTargetRequest request, CancellationToken cancellationToken = default)
    {
        var storageKey = StorageKeyFactory.Create(request);
        EnsureSafeStorageKey(storageKey);
        var uploadId = Guid.NewGuid();
        var uri = new Uri($"/api/v1/storage/local/uploads/{uploadId:N}", UriKind.Relative);
        return Task.FromResult(new SignedUploadTarget(uploadId, storageKey, uri, DateTimeOffset.UtcNow.AddMinutes(_options.SignedUrlLifetimeMinutes), request.RequiresMultipartUpload));
    }

    public Task<SignedDownloadTarget> CreateDownloadTargetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        EnsureSafeStorageKey(storageKey);
        var encoded = Uri.EscapeDataString(storageKey);
        var uri = new Uri($"/api/v1/storage/local/downloads/{encoded}", UriKind.Relative);
        return Task.FromResult(new SignedDownloadTarget(uri, DateTimeOffset.UtcNow.AddMinutes(_options.SignedUrlLifetimeMinutes)));
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

    private static void EnsureSafeStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey) || storageKey.Contains("..", StringComparison.Ordinal) || storageKey.Contains('\u005c'))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }
    }
}
