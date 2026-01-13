using BatchProcessor.Services;
using NCrontab;

namespace BatchProcessor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _cronExpression;
    private CrontabSchedule? _schedule;
    private DateTime _nextRun;

    public Worker(
        ILogger<Worker> logger, 
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _cronExpression = _configuration["BatchProcessor:Schedule"] ?? "0 1 * * *"; // Default: 1:00 AM daily
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BatchProcessor Worker starting at: {time}", DateTimeOffset.Now);
        
        // Parse cron expression
        try
        {
            _schedule = CrontabSchedule.Parse(_cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
            _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
            _logger.LogInformation("Scheduled to run at: {nextRun} (Cron: {cron})", _nextRun, _cronExpression);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid cron expression: {cron}", _cronExpression);
            return;
        }

        // For testing purposes, also run immediately on startup if configured
        var runOnStartup = _configuration.GetValue<bool>("BatchProcessor:RunOnStartup", false);
        if (runOnStartup)
        {
            _logger.LogInformation("Running batch process immediately (RunOnStartup=true)");
            await ProcessBatch(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            if (now >= _nextRun)
            {
                await ProcessBatch(stoppingToken);
                _nextRun = _schedule!.GetNextOccurrence(DateTime.Now);
                _logger.LogInformation("Next run scheduled at: {nextRun}", _nextRun);
            }

            // Check every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessBatch(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting batch processing at: {time}", DateTimeOffset.Now);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var xmlProcessor = scope.ServiceProvider.GetRequiredService<IXmlProcessorService>();
            
            var inputFolder = _configuration["BatchProcessor:InputFolder"] ?? "/data/input";
            var filePattern = _configuration["BatchProcessor:FilePattern"] ?? "access-log-*.xml";
            var archiveFolder = _configuration["BatchProcessor:ArchiveFolder"] ?? "/data/archive";
            
            await xmlProcessor.ProcessXmlFilesAsync(inputFolder, filePattern, archiveFolder, stoppingToken);
            
            _logger.LogInformation("Batch processing completed successfully at: {time}", DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch processing");
        }
    }
}
