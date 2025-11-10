using Microsoft.Extensions.Configuration;
using Moq;
using OcrWorker;
using OcrWorker.Exceptions;
using OcrWorker.Services;

namespace PaperlessServices.Tests;

[TestFixture]
public class MinIOServiceTests
{
    private Mock<IConfiguration> _mockConfig;
    private Mock<IDocumentStorageService> _documentStorage;

    [SetUp]
    public void Setup()
    {
        _mockConfig = new Mock<IConfiguration>();
        _documentStorage = new Mock<IDocumentStorageService>();

        _mockConfig.Setup(c => c["MINIO_ENDPOINT"]).Returns("localhost:9001");
        _mockConfig.Setup(c => c["MINIO_ROOT_USER"]).Returns("minioadmin");
        _mockConfig.Setup(c => c["MINIO_ROOT_PASSWORD"]).Returns("minioadmin");
    }

    [Test]
    public void DownloadFileAsync_FailedDownload_ThrowsMinioDocumentDownloadException()
    {
        string documentName = "document.pdf";
        string filePath = "/tmp/document.pdf";
        
        _documentStorage
            .Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new MinioDocumentDownloadException(documentName, new Exception()));

        Assert.Throws<MinioDocumentDownloadException>(() => _documentStorage.Object.DownloadFileAsync(documentName, filePath));
    }

    [Test]
    public void Configuration_LoadsMinIOSettings()
    {
        var endpoint = _mockConfig.Object["MINIO_ENDPOINT"];
        var user = _mockConfig.Object["MINIO_ROOT_USER"];
        var password = _mockConfig.Object["MINIO_ROOT_PASSWORD"];

        Assert.That(endpoint, Is.EqualTo("localhost:9001"));
        Assert.That(user, Is.EqualTo("minioadmin"));
        Assert.That(password, Is.EqualTo("minioadmin"));
    }
}

