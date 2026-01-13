using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BatchProcessor.Services;

public class XmlProcessorService : IXmlProcessorService
{
    private readonly ILogger<XmlProcessorService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public XmlProcessorService(ILogger<XmlProcessorService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task ProcessXmlFilesAsync(string inputFolder, string filePattern, string archiveFolder, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing XML files from: {folder} with pattern: {pattern}", inputFolder, filePattern);

        if (!Directory.Exists(inputFolder))
        {
            _logger.LogWarning("Input folder does not exist: {folder}", inputFolder);
            return;
        }

        // Ensure archive folder exists
        if (!Directory.Exists(archiveFolder))
        {
            Directory.CreateDirectory(archiveFolder);
            _logger.LogInformation("Created archive folder: {folder}", archiveFolder);
        }

        // Find all XML files matching the pattern
        var files = Directory.GetFiles(inputFolder, filePattern);
        _logger.LogInformation("Found {count} XML files to process", files.Length);

        foreach (var filePath in files)
        {
            try
            {
                await ProcessXmlFileAsync(filePath, archiveFolder, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {file}", filePath);
            }
        }
    }

    private async Task ProcessXmlFileAsync(string filePath, string archiveFolder, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing file: {file}", filePath);

        // Parse XML file
        var doc = XDocument.Load(filePath);
        var root = doc.Root;

        if (root?.Name != "AccessLog")
        {
            _logger.LogWarning("Invalid XML format in file: {file}. Expected root element 'AccessLog'", filePath);
            return;
        }

        var date = root.Attribute("date")?.Value;
        _logger.LogInformation("Processing access log for date: {date}", date);

        var accessRecords = root.Elements("Document")
            .Select(e => new
            {
                DocumentId = int.Parse(e.Attribute("id")?.Value ?? "0"),
                AccessCount = int.Parse(e.Attribute("accessCount")?.Value ?? "0")
            })
            .Where(r => r.DocumentId > 0)
            .ToList();

        _logger.LogInformation("Found {count} document access records", accessRecords.Count);

        // Update database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BatchDbContext>();

        foreach (var record in accessRecords)
        {
            try
            {
                var document = await dbContext.Documents
                    .FirstOrDefaultAsync(d => d.Id == record.DocumentId, cancellationToken);

                if (document != null)
                {
                    // Update access statistics
                    document.AccessCount = (document.AccessCount ?? 0) + record.AccessCount;
                    document.LastAccessDate = DateTime.UtcNow;
                    
                    _logger.LogDebug("Updated document {id}: AccessCount = {count}", 
                        document.Id, document.AccessCount);
                }
                else
                {
                    _logger.LogWarning("Document not found: {id}", record.DocumentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {id}", record.DocumentId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Database updated successfully");

        // Archive the processed file
        var fileName = Path.GetFileName(filePath);
        var archivePath = Path.Combine(archiveFolder, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}_{fileName}");
        File.Move(filePath, archivePath);
        _logger.LogInformation("File archived to: {archive}", archivePath);
    }
}
