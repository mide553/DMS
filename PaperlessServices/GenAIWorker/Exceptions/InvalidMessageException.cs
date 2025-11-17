namespace GenAIWorker.Exceptions
{
    public class InvalidMessageException : Exception
    {
        public InvalidMessageException(string item) : base($"Message missing valid '{item}'") { }
    }
}
