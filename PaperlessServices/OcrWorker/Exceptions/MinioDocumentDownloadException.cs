namespace OcrWorker.Exceptions
{
    public class MinioDocumentDownloadException : Exception
    {
        public MinioDocumentDownloadException(string filename, Exception ex) : base($"Failed to download document ({filename}) from MinIO", ex) { }
    }
}
