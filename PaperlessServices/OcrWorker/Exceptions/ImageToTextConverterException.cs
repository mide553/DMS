namespace OcrWorker.Exceptions
{
    public class ImageToTextConverterException : Exception
    {
        public ImageToTextConverterException(string filename) : base($"Failed to convert image to text ({filename})") { }
    }
}
