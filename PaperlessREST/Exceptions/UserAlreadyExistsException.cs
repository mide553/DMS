namespace PaperlessREST.Exceptions
{
    public enum RegisterConflictReason
    {
        Username,
        Email
    }

    public class UserAlreadyExistsException : Exception
    {
        public RegisterConflictReason Reason { get; }

        public UserAlreadyExistsException(RegisterConflictReason reason) : base($"{reason.ToString()} already exists") 
        {
            Reason = reason;
        }
    }
}
