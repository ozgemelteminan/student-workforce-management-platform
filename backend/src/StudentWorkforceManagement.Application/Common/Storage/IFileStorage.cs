namespace StudentWorkforceManagement.Application.Common.Storage;

public interface IFileStorage
{
    System.Threading.Tasks.Task<SignedUploadTarget> CreateUploadTargetAsync(UploadTargetRequest request, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SignedDownloadTarget> CreateDownloadTargetAsync(string storageKey, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<StoredFileMetadata?> GetMetadataAsync(string storageKey, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task SaveAsync(string storageKey, Stream content, string mimeType, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
