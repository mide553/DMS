using OcrWorker.Exceptions;

namespace PaperlessServices.Tests;

[TestFixture]
public class ExceptionTests
{
    [Test]
    public void GhostscriptPdfToImageConverterException_CreatesExceptionWithMessage()
    {
        string filename = "name.pdf";

        var exception = new GhostscriptPdfToImageConverterException(filename);

        Assert.That(exception.Message, Does.Contain($"Ghostscript failed to convert pdf to image ({filename})"));
    }

    [Test]
    public void ImageToTextConverterException_CreatesExceptionWithMessage()
    {
        string filename = "name.png";

        var exception = new ImageToTextConverterException(filename);

        Assert.That(exception.Message, Does.Contain($"Failed to convert image to text ({filename})"));
    }

    [Test]
    public void MinioDocumentDownloadException_CreatesExceptionWithMessage()
    {
        string filename = "name.pdf";

        string innerExceptionMessage = "Downloading failed";
        var innerException = new Exception(innerExceptionMessage);

        var exception = new MinioDocumentDownloadException(filename, innerException);

        Console.WriteLine(exception.Message);
        Assert.That(exception.Message, Does.Contain($"Failed to download document ({filename}) from MinIO"));
        Assert.That(exception.InnerException, Is.Not.Null);
        Assert.That(exception.InnerException.Message, Does.Contain(innerExceptionMessage));
    }

    [Test]
    public void OcrWorkerProcessException_CreatesExceptionWithInnerException()
    {
        string innerExceptionMessage = "Processing failed";
        var innerException = new Exception(innerExceptionMessage);

        var exception = new OcrWorkerProcessException(innerException);

        Console.WriteLine(exception);
        Assert.That(exception.Message, Does.Contain("Worker failed to process message"));
        Assert.That(exception.InnerException, Is.Not.Null);
        Assert.That(exception.InnerException.Message, Does.Contain(innerExceptionMessage));
    }

    [Test]
    public void UnsupportedFileExtensionException_CreatesExceptionWithMessage()
    {
        string unsupportedExtension = ".exe";

        var exception = new UnsupportedFileExtensionException(unsupportedExtension);
        
        Assert.That(exception.Message, Does.Contain($"Unsupported extension ({unsupportedExtension})"));
    }
}
