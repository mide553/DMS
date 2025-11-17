namespace GenAIWorker.Exceptions
{
    public class MissingConfigurationItemException : Exception
    {
        public MissingConfigurationItemException(string configItem) : base($"Missing {configItem}") { }
    }
}
