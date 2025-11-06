using Microsoft.AspNetCore.Mvc;
using PaperlessREST.Services.Interface;
using AutoMapper;
using PaperlessREST.Data;
using PaperlessREST.Exceptions;
using PaperlessModels.Models;
using PaperlessModels.DTOs;

namespace PaperlessREST.Services
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase, IDocumentController
    {
        private readonly ApplicationDBContext _context;
        private readonly IMapper _mapper;
        private readonly IDocumentStorageService _documentStorage;
        private readonly IMessageQueueService _queueService;
        private readonly ILogger<DocumentController> _logger;


        public DocumentController(ApplicationDBContext dbContext, IMapper mapper, IDocumentStorageService documentStorage, IMessageQueueService queueService, ILogger<DocumentController> logger)
        {
            _context = dbContext;
            _mapper = mapper;
            _documentStorage = documentStorage;
            _queueService = queueService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            _logger.LogInformation("Fetching all documents");
            List<Document> docs = _context.Documents.ToList();

            if (docs is null)
            {
                _logger.LogWarning($"No document found");
                return NotFound();    // 404 Not Found
            }

            return Ok(docs);    // 200 Ok
        }

        [HttpGet("{id}")]
        public IActionResult GetDocumentById([FromRoute] int id)
        {
            _logger.LogInformation($"Fetching document with ID {id}");

            if (id < 1)
            {
                _logger.LogError($"Invalid ID: {id}");
                throw new InvalidIdException(id);
            }

            var doc = _context.Documents.Find(id);

            if (doc is null)
            {
                _logger.LogWarning($"Document with ID {id} not found");
                return NotFound(); // 404 Not Found
            }

            return Ok(doc); // 200 Ok
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            _logger.LogInformation($"Uploading new document");

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            long fileSize = file.Length;
            if (fileSize > (5 * 1024 * 1024))
                return BadRequest("Maximum size can be 5MB");

            string fileName = file.FileName;
            
            // Save uploaded file temporaryly inside container
            var tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }

            // Save metadata to database
            Document docModel = new Document()
            {
                FileName = fileName,
                ByteSize = (int)fileSize
            };

            try
            {
                // Upload document to MinIO
                await _documentStorage.UploadFileAsync(file.FileName, tempPath);

                _context.Documents.Add(docModel); // TODO: Check if filename already exists
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save the changes");
                throw new FailedSaveException(ex);
            }
            finally
            {
                // Delete temp file after upload
                System.IO.File.Delete(tempPath);
            }

            // Add document to queue
            _logger.LogInformation($"Document {fileName} sent to queue");
            await _queueService.PublishAsync("ocr_queue", fileName);

            // Return created Document as DTO Object
            return CreatedAtAction(nameof(GetDocumentById), new { id = docModel.Id }, _mapper.Map<DocumentDto>(docModel));  // 201 Created
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteDocument([FromRoute] int id)
        {
            _logger.LogInformation($"Deleting document with ID {id}");

            if (id < 1)
            {
                _logger.LogError($"Invalid ID: {id}");
                throw new InvalidIdException(id);
            }

            var docModel = _context.Documents.FirstOrDefault(x => x.Id == id);

            if (docModel is null)
            {
                _logger.LogWarning($"Document with ID {id} not found");
                return NotFound();    // 404 Not Found
            }

            try
            {
                _context.Documents.Remove(docModel);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save the changes");
                throw new FailedSaveException(ex);
            }

            return NoContent(); // 204 No Content
        }

        [HttpPut("{id}")]
        public IActionResult UpdateDocument([FromRoute] int id, [FromBody] DocumentDto docDto)
        {
            _logger.LogInformation($"Updating document with ID {id}");

            if (id < 1)
            {
                _logger.LogError($"Invalid ID: {id}");
                throw new InvalidIdException(id);
            }

            var docModel = _context.Documents.FirstOrDefault(x => x.Id == id);

            if (docModel is null)
            {
                _logger.LogWarning($"Document with ID {id} not found");
                return NotFound();    // 404 Not Found
            }

            docModel.FileName = docDto.FileName;
            docModel.ByteSize = docDto.ByteSize;
            docModel.Summary = docDto.Summary;
            docModel.LastModified = docDto.LastModified;

            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save the changes");
                throw new FailedSaveException(ex);
            }

            // Return updated Document as DTO Object
            return Ok(_mapper.Map<DocumentDto>(docModel));  // 200 Ok
        }
    }
}
