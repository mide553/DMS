using PaperlessREST.Exceptions;
using Minio;
using Minio.DataModel.Args;

namespace PaperlessREST.Services
{
    public interface IDocumentStorageService
    {
        public Task UploadFileAsync(string documentName, string filePath);
        public Task DeleteFileAsync(string documentName);
    }
    
    public class MinIOService : IDocumentStorageService
    {
        private readonly IMinioClient _client;
        private readonly ILogger _logger;
        private readonly string _bucketName = "documents";

        public MinIOService(IConfiguration config, ILogger<MinIOService> logger)
        {
            string endpoint = config["MINIO_ENDPOINT"] ?? throw new MissingConfigurationItemException("MinIO Endpoint");
            string username = config["MINIO_ROOT_USER"] ?? throw new MissingConfigurationItemException("MinIO User");
            string password = config["MINIO_ROOT_PASSWORD"] ?? throw new MissingConfigurationItemException("MinIO Password");

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

        public async Task DeleteFileAsync(string documentName)
        {
            try
            {
                _logger.LogInformation($"Deleting file {documentName} from MinIO...");

                await _client.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(documentName));

                _logger.LogInformation($"Successfully deleted {documentName} from MinIO.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete {documentName} from MinIO.");
                throw;
            }
        }
    }
}
