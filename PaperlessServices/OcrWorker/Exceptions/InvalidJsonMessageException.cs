namespace OcrWorker.Exceptions
{
    public class InvalidJsonMessageException : Exception
    {
        public InvalidJsonMessageException(string json) : base($"Invalid JSON message: {json}") { }
    }
}
