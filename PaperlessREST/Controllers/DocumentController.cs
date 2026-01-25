using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PaperlessREST.Repositories;
using PaperlessREST.Exceptions;
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
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(IDocumentRepository documentRepository, ILogger<DocumentController> logger)
        {
            _documentRepository = documentRepository;
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
            try
            {
                int userId = GetUserId();
                List<OwnDocumentDto> docs = await _documentRepository.GetAllDocumentsAsync(userId);

                return Ok(docs);    // 200 Ok
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();  // 401 Unauthorized
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching document");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentById([FromRoute] int id)
        {
            if (id < 1)
            {
                _logger.LogWarning($"Invalid document ID: {id}");
                return BadRequest($"Invalid document ID: {id}");    // 400 Bad Request
            }

            try
            {
                int userId = GetUserId();
                DocumentDto doc = await _documentRepository.GetDocumentByIdAsync(id, userId);
                
                return Ok(doc); // 200 Ok
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();  // 401 Unauthorized
            }
            catch (ForbiddenContentException)
            {
                return Forbid();    // 403 Forbidden
            }
            catch (NotFoundException)
            {
                return NotFound();  // 404 Not Found
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error fetching document {id}");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
        }

        [HttpGet("search/{searchText}")]
        public async Task<IActionResult> SearchDocument([FromRoute] string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                _logger.LogWarning($"Invalid search text: {searchText}");
                return BadRequest($"Invalid search text: {searchText}");    // 400 Bad Request
            }

            try
            {
                int userId = GetUserId();
                List<DocumentDto> docs = await _documentRepository.SearchDocumentAsync(searchText, userId);
                
                return Ok(docs);   // 200 Ok
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();  // 401 Unauthorized
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error searching document");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
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
                Document doc = await _documentRepository.UploadDocumentAsync(file, userId);

                return CreatedAtAction(nameof(GetDocumentById), new { id = doc.Id }, doc);  // 201 Created
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();  // 401 Unauthorized
            }
            catch (FileAlreadyExistsException)
            {
                return Conflict($"File with name {file.FileName} already exists");  // 409 Conflict
            }
            catch (DocumentUploadException)
            {
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
                await _documentRepository.DeleteDocumentAsync(id, userId);
                
                return NoContent();     // 204 No Content
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();  // 401 Unauthorized
            }
            catch (ForbiddenActionException)
            {
                return Forbid();        // 403 Forbidden
            }
            catch (NotFoundException)
            {
                return NotFound();      // 404 Not Found
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
                DocumentDto doc = await _documentRepository.UpdateDocumentAsync(id, docDto, userId);

                // Return updated Document as DTO Object
                return Ok(doc); // 200 Ok
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();  // 401 Unauthorized
            }
            catch (ForbiddenActionException)
            {
                return Forbid();    // 403 Forbidden
            }
            catch (NotFoundException)
            {
                return NotFound();  // 404 Not Found
            }
            catch (UpdateException ex)
            {
                _logger.LogError(ex, $"Failed to update document {id}");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
        }
    }
}
