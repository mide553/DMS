namespace PaperlessREST.Exceptions
{
    public class DocumentNotFoundException : Exception
    {
        public DocumentNotFoundException(int id) : base($"Document with ID {id} not found") { }
    }
}
