namespace OcrWorker.Exceptions
{
    public class TesseractExtractorException : Exception
    {
        public TesseractExtractorException(Exception innerException) : base($"Error extracting text from document", innerException) { }
    }
}
