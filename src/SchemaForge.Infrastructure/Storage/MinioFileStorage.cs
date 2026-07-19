using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.Storage;

// MinIO (docker-compose) or, unmodified, any other S3-compatible store - never Azure/AWS-specific
// (Step 1 §9's confirmed decision). ForcePathStyle is required for MinIO specifically; AWS S3
// itself works with either style, so this doesn't cost portability if this ever pointed at real
// S3 later.
public sealed class MinioFileStorage : IFileStorage, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private readonly SemaphoreSlim _bucketEnsureLock = new(1, 1);
    private bool _bucketEnsured;

    public MinioFileStorage(IOptions<StorageSettings> settings)
    {
        var config = settings.Value;
        _bucketName = config.BucketName;

        _client = new AmazonS3Client(
            config.AccessKey,
            config.SecretKey,
            new AmazonS3Config
            {
                ServiceURL = config.Endpoint,
                ForcePathStyle = true,
                UseHttp = config.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            });
    }

    public async Task UploadAsync(
        string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType
            },
            cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _client.GetObjectAsync(
            new GetObjectRequest { BucketName = _bucketName, Key = key }, cancellationToken);

        return response.ResponseStream;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        _client.DeleteObjectAsync(
            new DeleteObjectRequest { BucketName = _bucketName, Key = key }, cancellationToken);

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured) return;

        await _bucketEnsureLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketEnsured) return;

            var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_client, _bucketName);
            if (!exists)
            {
                await _client.PutBucketAsync(_bucketName, cancellationToken);
            }

            _bucketEnsured = true;
        }
        finally
        {
            _bucketEnsureLock.Release();
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _bucketEnsureLock.Dispose();
    }
}
