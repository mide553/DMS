namespace GenAIWorker.Exceptions
{
    public class SummaryGeneratorException : Exception
    {
        public SummaryGeneratorException(Exception innerException) : base($"Failed to generate summary of document text", innerException) { }
    }
}
