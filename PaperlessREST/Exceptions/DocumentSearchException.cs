namespace PaperlessREST.Exceptions
{
    public class DocumentSearchException : Exception
    {
        public DocumentSearchException() : base("Elasticsearch query failed") { }
    }
}
