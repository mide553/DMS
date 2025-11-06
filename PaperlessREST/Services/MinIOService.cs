using Minio;
using Minio.DataModel.Args;

namespace PaperlessREST.Services
{
    public interface IDocumentStorageService
    {
        public Task UploadFileAsync(string documentName, string filePath);
    }
    
    public class MinIOService : IDocumentStorageService
    {
        private readonly IMinioClient _client;
        private readonly ILogger _logger;
        private readonly string _bucketName = "documents";

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

        public async Task UploadFileAsync(string documentName, string filePath)
        {
            bool exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if (!exists)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                _logger.LogInformation($"Created new Bucket: {_bucketName}");
            }

            // Upload file
            await _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(documentName)
                .WithFileName(filePath));

            _logger.LogInformation($"Uploaded {documentName} to MinIO (Bucket: {_bucketName})");
        }
    }
}
