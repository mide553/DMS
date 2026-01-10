namespace GenAIWorker.Exceptions
{
    public class EmptySummaryException : Exception
    {
        public EmptySummaryException() : base($"Generated summary was empty") { }
    }
}
