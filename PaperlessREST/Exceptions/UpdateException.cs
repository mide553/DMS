namespace PaperlessREST.Exceptions
{
    public class UpdateException : Exception
    {
        public UpdateException(string entity, int id, Exception innerException) : base($"Failed to update {entity} with ID {id}", innerException) { }
    }
}
