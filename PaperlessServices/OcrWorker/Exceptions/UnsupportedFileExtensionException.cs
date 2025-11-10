namespace OcrWorker.Exceptions
{
    public class UnsupportedFileExtensionException : Exception
    {
        public UnsupportedFileExtensionException(string extension) : base($"Unsupported extension ({extension})") { }
    }
}
