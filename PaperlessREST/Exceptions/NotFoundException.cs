namespace PaperlessREST.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string entity, int id) : base($"{entity} with ID {id} not found") { }
    }
}
