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

    public async Task<(bool IsHealthcare, string Answer)> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Ask Ollama to act as both a classifier and a responder and return a strict JSON object.
                var systemPrompt = """
                        You are a healthcare assistant AND a content classifier.

                        Instructions:
                        1) For every user input, determine whether it is a healthcare-related question.
                        2) If it is healthcare-related, provide a helpful, non-diagnostic answer. Always recommend consulting a qualified healthcare professional when appropriate.
                        3) If it is NOT healthcare-related, do NOT answer the question; instead return a short refusal message.
                        4) OUTPUT MUST BE a single JSON object and nothing else, with the exact keys:
                             {
                                 "isHealthcare": true|false,
                                 "answer": "<the assistant answer or refusal>"
                             }
                        5) Do not include any extra commentary, explanation, or markdown — only the JSON object.
                        6) Never provide definitive medical diagnoses; only provide general information and urge consulting a professional.

                        Examples:
                        User Question: "What is fever?"
                        Output: {"isHealthcare": true, "answer": "Fever is an elevation in body temperature; if high (e.g., 104°F) seek medical attention and stay hydrated. Consult a doctor for persistent or severe symptoms."}

                        User Question: "fever type?"
                        Output: {"isHealthcare": true, "answer": "Fever types include low-grade, moderate, and high fever; evaluate severity by temperature and symptoms and seek medical attention for high fevers or concerning symptoms."}

                        User Question: "What's the weather tomorrow?"
                        Output: {"isHealthcare": false, "answer": "This is outside healthcare scope."}

                        Note: Treat short/terse queries that include clear healthcare keywords (e.g., fever, temperature, degree, °F, °C, symptoms, doctor, pain) as healthcare-related even if phrased tersely.
                """;

        var finalPrompt = systemPrompt + "\n\nUser Question: " + prompt;

        var requestBody = new
        {
            model = "gemma:2b",
            prompt = finalPrompt,
            max_tokens = 512,
            temperature = 0.0,
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

        // Try to extract a JSON object from the response text. Ollama may return the raw JSON
        // or wrap it; attempt robust parsing.
        try
        {
            var trimmed = responseText.Trim();

            string jsonFragment = null!;

            if (trimmed.StartsWith("{"))
            {
                jsonFragment = trimmed;
            }
            else
            {
                // Find first and last brace to extract JSON object
                var first = trimmed.IndexOf('{');
                var last = trimmed.LastIndexOf('}');
                if (first >= 0 && last > first)
                {
                    jsonFragment = trimmed.Substring(first, last - first + 1);
                }
            }

            if (!string.IsNullOrWhiteSpace(jsonFragment))
            {
                using var doc = JsonDocument.Parse(jsonFragment);
                var root = doc.RootElement;

                // Try to extract the preferred contract either from the root JSON
                // or from a nested `response` field which may be a JSON string.
                bool TryExtract(JsonElement el, out bool isHealthcareOut, out string answerOut)
                {
                    isHealthcareOut = false;
                    answerOut = string.Empty;

                    if (el.ValueKind != JsonValueKind.Object)
                        return false;

                    if (el.TryGetProperty("isHealthcare", out var isH))
                    {
                        isHealthcareOut = isH.ValueKind == JsonValueKind.True;
                        if (el.TryGetProperty("answer", out var ans) && ans.ValueKind == JsonValueKind.String)
                        {
                            answerOut = ans.GetString() ?? string.Empty;
                        }
                        return true;
                    }

                    if (el.TryGetProperty("response", out var resp))
                    {
                        if (resp.ValueKind == JsonValueKind.String)
                        {
                            var inner = resp.GetString() ?? string.Empty;
                            try
                            {
                                using var innerDoc = JsonDocument.Parse(inner.Trim());
                                return TryExtract(innerDoc.RootElement, out isHealthcareOut, out answerOut);
                            }
                            catch { /* ignore parse errors and continue */ }
                        }
                        else if (resp.ValueKind == JsonValueKind.Object)
                        {
                            return TryExtract(resp, out isHealthcareOut, out answerOut);
                        }
                    }

                    return false;
                }

                if (TryExtract(root, out var extractedHealthcare, out var extractedAnswer))
                {
                    return (extractedHealthcare, extractedAnswer.Trim());
                }

                // If JSON exists but doesn't contain the required `isHealthcare` key,
                // treat it as non-healthcare to be conservative.
                _logger.LogWarning("Ollama returned JSON without isHealthcare key; refusing as non-healthcare.");
                return (false, "This is outside healthcare scope.");
            }

            // No JSON fragment found — model did not follow the JSON contract. Refuse by default.
            _logger.LogWarning("Ollama did not return JSON. Refusing request as non-healthcare.");
            return (false, "This is outside healthcare scope.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Ollama JSON response, returning raw text.");
        }

        // As a last resort, try to strip a JSON object from the response and return any inner text,
        // otherwise return the full response as the answer and assume healthcare.
        var fallback = responseText.Trim();
        // If response starts with { and contains "answer" or "response", try a loose extraction
        try
        {
            var first = fallback.IndexOf('{');
            var last = fallback.LastIndexOf('}');
            if (first >= 0 && last > first)
            {
                var frag = fallback.Substring(first, last - first + 1);
                using var doc = JsonDocument.Parse(frag);
                var root = doc.RootElement;
                if (root.TryGetProperty("answer", out var a) && a.ValueKind == JsonValueKind.String)
                {
                    return (true, a.GetString()?.Trim() ?? fallback);
                }
                if (root.TryGetProperty("response", out var r) && r.ValueKind == JsonValueKind.String)
                {
                    return (true, r.GetString()?.Trim() ?? fallback);
                }
            }
        }
        catch { }

        return (true, fallback);
    }
}
