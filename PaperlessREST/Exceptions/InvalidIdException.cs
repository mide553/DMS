namespace PaperlessREST.Exceptions
{
    public class InvalidIdException : Exception
    {
        public InvalidIdException(int id) : base($"Invalid ID: {id}") { }
    }
}
