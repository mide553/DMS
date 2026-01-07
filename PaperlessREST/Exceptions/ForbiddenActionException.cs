namespace PaperlessREST.Exceptions
{
    public class ForbiddenActionException : Exception
    {
        public ForbiddenActionException(string content, int contentId, int userId) : base($"User with ID {userId} is forbidden to perform action on {content} with ID {contentId}") { }
    }
}
