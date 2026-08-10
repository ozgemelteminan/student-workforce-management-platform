namespace StudentWorkforceManagement.Application.Common.Storage;

public interface IFileStorage
{
    Task<SignedUploadTarget> CreateUploadTargetAsync(UploadTargetRequest request, CancellationToken cancellationToken = default);
    Task<SignedDownloadTarget> CreateDownloadTargetAsync(string storageKey, CancellationToken cancellationToken = default);
}
