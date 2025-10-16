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
        private readonly IMessageQueueService _queueService;
        private readonly ILogger<DocumentController> _logger;


        public DocumentController(ApplicationDBContext dbContext, IMapper mapper, IMessageQueueService queueService, ILogger<DocumentController> logger)
        {
            _context = dbContext;
            _mapper = mapper;
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

        [HttpPost]
        public async Task<IActionResult> UploadDocument([FromBody] DocumentDto docDto)
        {
            _logger.LogInformation($"Uploading new document");
            Document docModel = _mapper.Map<Document>(docDto);

            // Save document to database
            try
            {
                _context.Documents.Add(docModel);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save the changes");
                throw new FailedSaveException(ex);
            }

            // Add document to queue
            _logger.LogInformation($"Document {docModel.Name} sent to queue");
            await _queueService.PublishAsync("ocr_queue", docModel);

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

            docModel.Name = docDto.Name;
            docModel.Filetype = docDto.Filetype;
            docModel.ByteSize = docDto.ByteSize;
            docModel.CreatedAt = docDto.CreatedAt;
            docModel.UpdatedAt = docDto.UpdatedAt;
            
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
