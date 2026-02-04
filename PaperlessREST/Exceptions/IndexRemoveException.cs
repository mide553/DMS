namespace PaperlessREST.Exceptions
{
    public class IndexRemoveException : Exception
    {
        public IndexRemoveException() : base("Failed to remove index") { }
    }
}
