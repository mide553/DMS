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
                throw new EmptyTextException();
            }

            try
            {
                // Request
                string prompt = $"Summarize the following document:\n{text}";
                var response = await RequestSummary(prompt);

                // Get summary
                using var doc = JsonDocument.Parse(response.Content!);
                string summary = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()!;

                if (String.IsNullOrEmpty(summary))
                {
                    throw new EmptySummaryException();
                }

                // Check quality
                int MIN_LENGTH = 15;
                if (summary.Length <  MIN_LENGTH)
                {
                    throw new SummaryQualityException();
                }

                _logger.LogInformation($"Finished summarizing text");
                return summary;
            }
            catch (EmptySummaryException ex)
            {
                _logger.LogError(ex, $"GenAI returned empty summary");
                throw new SummaryGeneratorException(ex);
            }
            catch (SummaryQualityException ex)
            {
                _logger.LogError(ex, $"Generative summary was not a real summary");
                throw new SummaryGeneratorException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to summarize text");
                throw new SummaryGeneratorException(ex);
            }
        }

        public async Task<RestResponse> RequestSummary(string prompt)
        {
            try
            {
                string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
                using var client = new RestClient(endpoint);
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
                    _logger.LogError("Gemini API response failed");
                    throw new GeminiApiException();
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini request failed");
                throw new GeminiRequestException();
            }
        }
    }
}
