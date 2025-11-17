namespace GenAIWorker.Exceptions
{
    public class EmptyTextException : Exception
    {
        public EmptyTextException() : base($"Text to summarize was empty") { }
    }
}
