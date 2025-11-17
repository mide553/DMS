namespace PaperlessREST.Exceptions
{
    public class DocumentUploadException : Exception
    {
        public DocumentUploadException(string fileName, Exception innerException) : base($"Failed to upload document {fileName}", innerException) { }
    }
}
