namespace PaperlessREST.Exceptions
{
    public class UpdateSummaryException : Exception
    {
        public UpdateSummaryException(Exception innerException) : base($"Failed to update document summary", innerException) { }
    }
}
