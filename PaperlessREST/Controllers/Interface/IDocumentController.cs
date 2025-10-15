using Microsoft.AspNetCore.Mvc;
using PaperlessModels.DTOs;

namespace PaperlessREST.Services.Interface
{
    public interface IDocumentController
    {
        public IActionResult GetAllDocuments();
        public IActionResult GetDocumentById(int id);
        public Task<IActionResult> UploadDocument(DocumentDto docDto);
        public IActionResult DeleteDocument(int id);
        public IActionResult UpdateDocument(int id, DocumentDto docDto);
    }
}
