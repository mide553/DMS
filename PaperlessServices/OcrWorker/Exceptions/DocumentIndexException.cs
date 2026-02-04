namespace OcrWorker.Exceptions
{
    public class DocumentIndexException : Exception
    {
        public DocumentIndexException() : base("Failed to index document") { }
    }
}
