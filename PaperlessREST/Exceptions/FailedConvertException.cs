namespace PaperlessREST.Exceptions
{
    public class FailedConvertException : Exception
    {
        public FailedConvertException(string convertFrom, string convertTo, Exception exception) 
            : base($"Failed to convert from {convertFrom} to {convertTo}: {exception.Message}") { }
    }
}
