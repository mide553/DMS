using PaperlessREST.Services.Interface;
using PaperlessREST.Data;
using PaperlessREST.Model;
using Microsoft.AspNetCore.Mvc;

namespace PaperlessREST.Services
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase, IDocumentController
    {
        private readonly ApplicationDBContext _context;
        public DocumentController(ApplicationDBContext dbContext)
        {
            _context = dbContext;
        }

        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            List<Document> docs = _context.Documents.ToList();
            
            if (docs is null) return NotFound();
            return Ok(docs);
        }

        [HttpGet("{id}")]
        public IActionResult GetDocumentById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public IActionResult AddDocument(Document document)
        {
            throw new NotImplementedException();
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
