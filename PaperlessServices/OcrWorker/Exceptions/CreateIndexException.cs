namespace OcrWorker.Exceptions
{
    public class IndexCreationException : Exception
    {
        public IndexCreationException() : base("Failed to create an index") { }
    }
}
