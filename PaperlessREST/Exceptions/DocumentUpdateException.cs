namespace PaperlessREST.Exceptions
{
    public class DocumentUpdateException : Exception
    {
        public DocumentUpdateException(int documentId, Exception innerException) : base($"Failed to update document with ID {documentId}", innerException) { }
    }
}
