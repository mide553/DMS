using Minio;
using Minio.DataModel.Args;

namespace PaperlessREST.Services
{
    public interface IDocumentStorageService
    {
        public Task UploadFileAsync(string bucketName, string documentName, string filePath);
    }
    
    public class MinIOService : IDocumentStorageService
    {
        private readonly IMinioClient _client;
        private readonly ILogger _logger;

        public MinIOService(ILogger<MinIOService> logger)
        {
            var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
            var username = Environment.GetEnvironmentVariable("MINIO_ROOT_USER");
            var password = Environment.GetEnvironmentVariable("MINIO_ROOT_PASSWORD");

            _client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(username, password)
                .Build();

            _logger = logger;
        }

        public async Task UploadFileAsync(string bucketName, string documentName, string filePath)
        {
            bool found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                _logger.LogInformation($"Created new Bucket: {bucketName}");
            }

            // Upload file
            await _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(documentName)
                .WithFileName(filePath));

            _logger.LogInformation($"Uploaded {documentName} to MinIO (Bucket: {bucketName})");
        }
    }
}
