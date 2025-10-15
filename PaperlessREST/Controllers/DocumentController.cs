using Microsoft.AspNetCore.Mvc;
using PaperlessREST.Services.Interface;
using AutoMapper;
using PaperlessREST.Data;
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

        public DocumentController(ApplicationDBContext dbContext, IMapper mapper, IMessageQueueService queueService)
        {
            _context = dbContext;
            _mapper = mapper;
            _queueService = queueService;
        }

        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            List<Document> docs = _context.Documents.ToList();

            if (docs is null) return NotFound();    // 404 Not Found
            return Ok(docs);    // 200 Ok
        }

        [HttpGet("{id}")]
        public IActionResult GetDocumentById([FromRoute] int id)
        {
            var doc = _context.Documents.Find(id);

            if (doc is null) return NotFound(); // 404 Not Found
            return Ok(doc); // 200 Ok
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument([FromBody] DocumentDto docDto)
        {
            Document docModel = _mapper.Map<Document>(docDto);

            // Add document to queue
            await _queueService.PublishAsync("ocr_queue", docModel);

            // Save document to database
            _context.Documents.Add(docModel);
            _context.SaveChanges();

            // Return created Document as DTO Object
            return CreatedAtAction(nameof(GetDocumentById), new { id = docModel.Id }, _mapper.Map<DocumentDto>(docModel));  // 201 Created
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteDocument([FromRoute] int id)
        {
            var docModel = _context.Documents.FirstOrDefault(x => x.Id == id);

            if (docModel is null) return NotFound();    // 404 Not Found

            _context.Documents.Remove(docModel);
            _context.SaveChanges();

            return NoContent(); // 204 No Content
        }

        [HttpPut("{id}")]
        public IActionResult UpdateDocument([FromRoute] int id, [FromBody] DocumentDto docDto)
        {
            var docModel = _context.Documents.FirstOrDefault(x => x.Id == id);

            if (docModel is null) return NotFound();    // 404 Not Found

            docModel.Name = docDto.Name;
            docModel.Filetype = docDto.Filetype;
            docModel.ByteSize = docDto.ByteSize;
            docModel.CreatedAt = docDto.CreatedAt;
            docModel.UpdatedAt = docDto.UpdatedAt;
            _context.SaveChanges();

            // Return updated Document as DTO Object
            return Ok(_mapper.Map<DocumentDto>(docModel));  // 200 Ok
        }
    }
}
