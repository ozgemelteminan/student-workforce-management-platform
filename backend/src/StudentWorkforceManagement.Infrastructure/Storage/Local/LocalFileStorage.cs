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
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
        return Task.FromResult<StoredFileMetadata?>(new StoredFileMetadata(storageKey, info.Length, mimeType, null));
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
