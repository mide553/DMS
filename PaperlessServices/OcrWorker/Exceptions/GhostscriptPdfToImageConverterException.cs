namespace OcrWorker.Exceptions
{
    public class GhostscriptPdfToImageConverterException : Exception
    {
        public GhostscriptPdfToImageConverterException(string filename) : base($"Ghostscript failed to convert pdf to image ({filename})") { }
    }
}
