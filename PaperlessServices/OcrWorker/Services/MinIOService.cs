using Minio;
using Minio.DataModel.Args;

namespace OcrWorker.Services
{
    public interface IDocumentStorageService
    {
        public Task DownloadFileAsync(string documentName, string filePath);
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

        public async Task DownloadFileAsync(string documentName, string filePath)
        {
            var file = await _client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(documentName)
                .WithFile(filePath));

            _logger.LogInformation($"Downloaded {documentName} from MinIO ({filePath})");
        }
    }
}
