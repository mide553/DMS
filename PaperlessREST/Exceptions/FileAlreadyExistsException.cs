namespace PaperlessREST.Exceptions
{
    public class FileAlreadyExistsException : Exception
    {
        public FileAlreadyExistsException(string fileName) : base($"File {fileName} already exists") { }
    }
}
