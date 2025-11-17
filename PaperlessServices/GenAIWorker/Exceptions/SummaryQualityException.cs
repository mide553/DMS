namespace GenAIWorker.Exceptions
{
    public class SummaryQualityException : Exception
    {
        public SummaryQualityException() : base($"Generative summary was not a real summary") { }
    }
}
