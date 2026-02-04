# BatchProcessor - Access Log Batch Service

This service processes daily XML files containing document access statistics from external systems.

## Features

- **Scheduled Processing**: Runs daily at 01:00 AM (configurable via cron expression)
- **XML File Processing**: Reads access log XML files and updates database
- **Automatic Archiving**: Moves processed files to archive folder
- **Configurable**: Input folder, file pattern, and schedule are all configurable

## XML Format

The service expects XML files in the following format:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<AccessLog date="2026-01-13">
  <Document id="1" accessCount="15" />
  <Document id="2" accessCount="8" />
  <Document id="3" accessCount="23" />
</AccessLog>
```

- **Root element**: `<AccessLog>` with `date` attribute (YYYY-MM-DD)
- **Child elements**: `<Document>` with `id` and `accessCount` attributes

## Configuration

Edit `appsettings.json` to configure:

```json
{
  "BatchProcessor": {
    "Schedule": "0 1 * * *",            // Cron expression (1:00 AM daily)
    "RunOnStartup": true,               // Run immediately on startup (for testing)
    "InputFolder": "/data/input",       // Folder to read XML files from
    "FilePattern": "access-log-*.xml",  // File pattern to match
    "ArchiveFolder": "/data/archive"    // Folder to archive processed files
  }
}
```

### Cron Expression Examples

- `0 1 * * *` - Daily at 1:00 AM
- `0 2 * * *` - Daily at 2:00 AM
- `0 0 * * 0` - Weekly on Sunday at midnight
- `0 */6 * * *` - Every 6 hours

## Sample Files

Sample XML file provided:
- `access-log-2026-01-12.xml`

**Testing the Batch Process**:
1. Copy sample XML files to the batch input volume:
   ```bash
   docker cp PaperlessServices/BatchProcessor/access-log-2026-01-12.xml BatchProcessor:/data/input/
   ```

2. Check the BatchProcessor logs:
   ```bash
   docker logs BatchProcessor
   ```

3. Query the database to verify access statistics:
   ```sql
   SELECT "Id", "FileName", "AccessCount", "LastAccessDate" FROM "Documents" WHERE "AccessCount" > 0;
   ```

4. Verify archived files:
   ```bash
   docker exec BatchProcessor ls -la /data/archive
   ```
