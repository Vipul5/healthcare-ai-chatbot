using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HealthcareChatbot.Api.Services;

public class OllamaClient : IOllamaClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaClient> _logger;

    public OllamaClient(HttpClient http, ILogger<OllamaClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string?> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var systemPrompt = """
            You are a healthcare assistant.

            Rules:
            - Answer only healthcare-related questions
            - If question is unrelated, refuse politely
            - Do not diagnose diseases
            - Recommend consulting doctors when necessary
            """;

        var finalPrompt = $"{systemPrompt}\n\nUser Question: {prompt}";

      var requestBody = new
        {
            model = "gemma:2b",
            prompt = finalPrompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = "http://localhost:11434/api/generate";

        _logger.LogInformation("Calling Ollama at {Url}", url);

        var response = await _http.PostAsync(url, content, cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation("Ollama Response: {Response}", responseText);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Ollama API Error: {response.StatusCode}\n{responseText}");
        }

        try
        {
            using var doc = JsonDocument.Parse(responseText);
          var text = doc.RootElement
                    .GetProperty("response")
                    .GetString();

                return text;
           
        }
        catch (JsonException)
        {
            // Fall through and return raw text
        }

        return responseText;
    }
}
