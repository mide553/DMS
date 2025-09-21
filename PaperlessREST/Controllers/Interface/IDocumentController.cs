using Microsoft.AspNetCore.Mvc;
using PaperlessREST.Model;

namespace PaperlessREST.Services.Interface
{
    public interface IDocumentController
    {
        public IActionResult GetAllDocuments();
        public IActionResult GetDocumentById(int id);
        public IActionResult AddDocument(Document document);
        public IActionResult DeleteDocument(int id);
        public IActionResult UpdateDocument(int id, Document document);
    }
}
