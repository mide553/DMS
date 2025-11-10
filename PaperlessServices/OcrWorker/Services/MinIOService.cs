using Minio;
using Minio.DataModel.Args;
using OcrWorker.Exceptions;

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

        public MinIOService(IConfiguration config, ILogger<MinIOService> logger)
        {
            var endpoint = config["MINIO_ENDPOINT"];
            var username = config["MINIO_ROOT_USER"];
            var password = config["MINIO_ROOT_PASSWORD"];

            _client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(username, password)
                .Build();

            _logger = logger;
        }

        public async Task DownloadFileAsync(string documentName, string filePath)
        {
            try
            {
                var file = await _client.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(documentName)
                    .WithFile(filePath));

                _logger.LogInformation($"Downloaded {documentName} from MinIO ({filePath})");
            } catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download file from MinIO");
                throw new MinioDocumentDownloadException(documentName, ex);
            }
        }
    }
}
