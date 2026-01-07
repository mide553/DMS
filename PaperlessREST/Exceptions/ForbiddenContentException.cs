namespace PaperlessREST.Exceptions
{
    public class ForbiddenContentException : Exception
    {
        public ForbiddenContentException(string content, int contentId, int userId) : base($"User with ID {userId} is forbidden to see content of {content} with ID {contentId}") { }
    }
}
