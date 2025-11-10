using Microsoft.Extensions.Configuration;
using Moq;
using OcrWorker.Exceptions;
using OcrWorker.Services;

namespace PaperlessServices.Tests;

[TestFixture]
public class TesseractTests
{
    private Mock<IDocumentExtractorService> _documentExtractor;

    [SetUp]
    public void Setup()
    {
        _documentExtractor = new Mock<IDocumentExtractorService>();
    }

    [Test]
    public void Ocr_Extracts_Text_From_Image()
    {
        _documentExtractor
            .Setup(x => x.ExtractDocument(It.IsAny<string>()))
            .Returns("Hello");

        string text = _documentExtractor.Object.ExtractDocument("filepath");

        Assert.That(text, Does.Contain("Hello"));
    }
}

