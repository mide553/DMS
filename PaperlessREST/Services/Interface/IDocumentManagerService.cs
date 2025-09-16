using Model;

namespace PaperlessREST.Services.Interface
{
    public interface IDocumentManagerService
    {
        public Document GetDocument(int id);
        public void AddDocument(Document document);
        public void DeleteDocument(int id);
        public void UpdateDocument(int id, Document document);
    }
}
