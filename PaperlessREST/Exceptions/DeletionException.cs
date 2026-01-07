namespace PaperlessREST.Exceptions
{
    public class DeletionException : Exception
    {
        public DeletionException(string entity, int id, Exception innerException) : base($"Failed to delete {entity} with ID {id}", innerException) { }
    }
}
