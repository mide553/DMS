namespace PaperlessREST.Exceptions
{
    public class DocumentDeletionException : Exception
    {
        public DocumentDeletionException(int documentId, Exception innerException) : base($"Failed to delete document with ID {documentId}", innerException) { }
    }
}
