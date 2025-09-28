using Microsoft.AspNetCore.Mvc;
using PaperlessREST.DTOs;

namespace PaperlessREST.Services.Interface
{
    public interface IDocumentController
    {
        public IActionResult GetAllDocuments();
        public IActionResult GetDocumentById(int id);
        public IActionResult AddDocument(DocumentDto docDto);
        public IActionResult DeleteDocument(int id);
        public IActionResult UpdateDocument(int id, DocumentDto docDto);
    }
}
