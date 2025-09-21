using PaperlessREST.Services.Interface;
using PaperlessREST.Data;
using PaperlessREST.Model;
using PaperlessREST.DTOs;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace PaperlessREST.Services
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase, IDocumentController
    {
        private readonly ApplicationDBContext _context;
        private readonly IMapper _mapper;
        public DocumentController(ApplicationDBContext dbContext, IMapper mapper)
        {
            _context = dbContext;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            List<Document> docs = _context.Documents.ToList();
            
            if (docs is null) return NotFound();
            return Ok(docs);
        }

        [HttpGet("{id}")]
        public IActionResult GetDocumentById([FromRoute] int id)
        {
            var doc = _context.Documents.Find(id);
            
            if (doc is null) return NotFound();
            return Ok(doc);
        }

        [HttpPost]
        public IActionResult AddDocument([FromBody] DocumentDto docDto)
        {
            var docModel = _mapper.Map<Document>(docDto);
            _context.Documents.Add(docModel);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetDocumentById), new { id = docModel.Id }, _mapper.Map<DocumentDto>(docModel));
        }

        [HttpPut]
        public IActionResult DeleteDocument(int id)
        {
            throw new NotImplementedException();
        }

        [HttpDelete]
        public IActionResult UpdateDocument(int id, Document document)
        {
            throw new NotImplementedException();
        }
    }
}
