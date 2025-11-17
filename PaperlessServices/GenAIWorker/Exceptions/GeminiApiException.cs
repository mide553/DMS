namespace GenAIWorker.Exceptions
{
    public class GeminiApiException : Exception
    {
        public GeminiApiException() : base($"Gemini API error") { }
    }
}
