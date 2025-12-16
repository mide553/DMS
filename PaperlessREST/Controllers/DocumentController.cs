using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PaperlessREST.Exceptions;
using PaperlessREST.Services;
using PaperlessModels.Models;
using PaperlessModels.DTOs;

namespace PaperlessREST.Controllers
{
    public interface IDocumentController
    {
        public Task<IActionResult> GetAllDocuments();
        public Task<IActionResult> GetDocumentById(int id);
        public Task<IActionResult> UploadDocument(IFormFile file);
        public Task<IActionResult> DeleteDocument(int id);
        public Task<IActionResult> UpdateDocument(int id, DocumentDto docDto);
    }

    [ApiController]
    [Route("api/documents")]
    [Authorize]
    public class DocumentController : ControllerBase, IDocumentController
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDocuments()
        {
            int userId = GetUserId();
            List<DocumentDto> docs = await _documentService.GetAllDocumentsAsync(userId);

            if (docs is null || docs.Count == 0)
            {
                _logger.LogWarning($"No document found for user {userId}");
                return Ok(new List<Document>());    // 200 Ok with empty list
            }

            return Ok(docs);    // 200 Ok
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentById([FromRoute] int id)
        {
            if (id < 1)
            {
                _logger.LogWarning($"Invalid document ID: {id}");
                return BadRequest($"Invalid document ID: {id}"); // 400 Bad Request
            }

            int userId = GetUserId();
            DocumentDto doc = await _documentService.GetDocumentByIdAsync(id, userId);

            if (doc is null)
            {
                _logger.LogWarning($"Document with ID {id} not found or unauthorized");
                return NotFound();  // 404 Not Found
            }

            return Ok(doc); // 200 Ok
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file uploaded");
                return BadRequest("No file uploaded");  // 400 Bad Request
            }

            const long maxFileSize = 5 * 1024 * 1024;   // 5 MB
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning($"Uploaded file exceeds size limit: {file.Length} bytes");
                return BadRequest("Maximum file size is 5MB");  // 400 Bad Request
            }

            try
            {
                int userId = GetUserId();
                Document doc = await _documentService.UploadDocumentAsync(file, userId);

                return CreatedAtAction(nameof(GetDocumentById), new { id = doc.Id }, doc);  // 201 Created
            }
            catch (FileAlreadyExistsException)
            {
                return Conflict($"File with name {file.FileName} already exists");  // 409 Conflict
            }
            catch (DocumentUploadException ex)
            {
                _logger.LogError(ex, "Failed to upload document");
                return StatusCode(500, "An error occurred while uploading the document");   // 500 Internal Server Error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while uploading document");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument([FromRoute] int id)
        {
            if (id < 1)
            {
                _logger.LogWarning($"Invalid document ID: {id}");
                return BadRequest($"Invalid document ID: {id}");    // 400 Bad Request
            }

            try
            {
                int userId = GetUserId();
                await _documentService.DeleteDocumentAsync(id, userId);
                return NoContent(); // 204 No Content
            }
            catch (DocumentNotFoundException)
            {
                return NotFound();  // 404 Not Found
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();    // 403 Forbidden
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error deleting document {id}");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument([FromRoute] int id, [FromBody] DocumentDto docDto)
        {
            if (id < 1)
            {
                _logger.LogWarning($"Invalid document ID: {id}");
                return BadRequest($"Invalid document ID: {id}");    // 400 Bad Request
            }

            if (docDto is null)
            {
                _logger.LogWarning($"DocumentDto is null for ID: {id}");
                return BadRequest("Document data must be provided");    // 400 Bad Request
            }

            try
            {
                int userId = GetUserId();
                DocumentDto doc = await _documentService.UpdateDocumentAsync(id, docDto, userId);

                // Return updated Document as DTO Object
                return Ok(doc); // 200 Ok
            }
            catch (DocumentNotFoundException)
            {
                return NotFound();  // 404 Not Found
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();    // 403 Forbidden
            }
            catch (DocumentUpdateException ex)
            {
                _logger.LogError(ex, $"Failed to update document {id}");
                return StatusCode(500, "Failed to update document due to an internal error");   // 500 Internal Server Error
            }
        }
    }
}
