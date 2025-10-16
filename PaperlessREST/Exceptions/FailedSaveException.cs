namespace PaperlessREST.Exceptions
{
    public class FailedSaveException : Exception
    {
        public FailedSaveException(Exception exception) : base($"Failed to save changes: {exception.Message}") { }
    }
}
