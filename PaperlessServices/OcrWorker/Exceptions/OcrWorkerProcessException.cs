namespace OcrWorker.Exceptions
{
    public class OcrWorkerProcessException : Exception
    {
        public OcrWorkerProcessException(Exception exception) : base($"Worker failed to process message: {exception}") { }
    }
}
