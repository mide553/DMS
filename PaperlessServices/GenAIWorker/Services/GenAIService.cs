using GenAIWorker.Exceptions;
using RestSharp;
using System.Text.Json;

namespace GenAIWorker.Services
{
    public interface ISummarizer
    {
        public Task<string> SummarizeTextAsync(string text);
    }
    public class GenAIService : ISummarizer
    {
        private readonly string _apiKey;
        ILogger<GenAIService> _logger;

        public GenAIService(IConfiguration config, ILogger<GenAIService> logger) 
        {
            _apiKey = config["GEMINI_API_KEY"] ?? throw new MissingConfigurationItemException("Gemini API Key");
            _logger = logger;
        }

        public async Task<string> SummarizeTextAsync(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                _logger.LogError("Failed to summarize empty text");
                throw new Exception("Failed to summarize empty text");
            }

            try
            {
                string prompt = $"Summarize the following document:\n{text}";

                var client = new RestClient("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent");
                var request = new RestRequest();
                request.AddQueryParameter("key", _apiKey);
                request.AddJsonBody(new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    }
                });

                _logger.LogInformation("Summarizing text...");
                var response = await client.PostAsync(request);
                if (!response.IsSuccessful)
                {
                    throw new Exception($"Gemini API error: {response.Content}");
                }

                using var doc = JsonDocument.Parse(response.Content!);
                string summary = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()!;

                _logger.LogInformation($"Finished summarizing text:\n{summary}");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini request failed");
                throw new Exception("Gemini request failed");
            }
        }
    }
}
