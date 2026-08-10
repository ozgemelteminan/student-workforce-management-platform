using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Storage;

namespace StudentWorkforceManagement.Infrastructure.Storage.ObjectStorage;

public sealed class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _client;
    private readonly StorageOptions _options;

    public S3FileStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_options.S3.Region),
            ForcePathStyle = _options.S3.ForcePathStyle
        };
        if (!string.IsNullOrWhiteSpace(_options.S3.ServiceUrl))
        {
            config.ServiceURL = _options.S3.ServiceUrl;
        }
        _client = new AmazonS3Client(new BasicAWSCredentials(_options.S3.AccessKey, _options.S3.SecretKey), config);
    }

    public Task<SignedUploadTarget> CreateUploadTargetAsync(UploadTargetRequest request, CancellationToken cancellationToken = default)
    {
        var storageKey = StorageKeyFactory.Create(request);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.SignedUrlLifetimeMinutes);
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.S3.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = request.MimeType
        });
        return Task.FromResult(new SignedUploadTarget(Guid.NewGuid(), storageKey, new Uri(url), expiresAt, request.RequiresMultipartUpload));
    }

    public Task<SignedDownloadTarget> CreateDownloadTargetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.SignedUrlLifetimeMinutes);
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.S3.BucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime
        });
        return Task.FromResult(new SignedDownloadTarget(new Uri(url), expiresAt));
    }

    public async Task<StoredFileMetadata?> GetMetadataAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.S3.BucketName,
                Key = storageKey
            }, cancellationToken);
            return new StoredFileMetadata(storageKey, response.ContentLength, response.Headers.ContentType, response.ETag?.Trim('"'));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
