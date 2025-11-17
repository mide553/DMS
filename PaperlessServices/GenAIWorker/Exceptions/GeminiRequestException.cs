namespace GenAIWorker.Exceptions
{
    public class GeminiRequestException : Exception
    {
        public GeminiRequestException() : base($"Gemini request error") { }
    }
}
