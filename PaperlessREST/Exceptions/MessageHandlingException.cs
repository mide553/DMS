namespace PaperlessREST.Exceptions
{
    public class MessageHandlingException : Exception
    {
        public MessageHandlingException(Exception innerException) : base($"Error while handling message", innerException) { }
    }
}
