namespace GenAIWorker.Exceptions
{
    public class GenaiWorkerProcessException : Exception
    {
        public GenaiWorkerProcessException(Exception innerException) : base($"Failed to summarize document text", innerException) { }
    }
}
