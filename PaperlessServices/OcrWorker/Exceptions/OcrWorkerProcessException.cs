namespace OcrWorker.Exceptions
{
    public class OcrWorkerProcessException : Exception
    {
        public OcrWorkerProcessException(Exception ex) : base($"Worker failed to process message", ex) { }
    }
}
