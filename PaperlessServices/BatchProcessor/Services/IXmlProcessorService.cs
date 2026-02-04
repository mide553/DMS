namespace BatchProcessor.Services;

public interface IXmlProcessorService
{
    Task ProcessXmlFilesAsync(string inputFolder, string filePattern, string archiveFolder, CancellationToken cancellationToken);
}
