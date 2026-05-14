using Microsoft.AspNetCore.Mvc;
using HealthcareChatbot.Api.Models;
using HealthcareChatbot.Api.Services;

namespace HealthcareChatbot.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IHealthcareClassifier _classifier;
    private readonly IOllamaClient _ollama;

    public ChatController(IHealthcareClassifier classifier, IOllamaClient ollama)
    {
        _classifier = classifier;
        _ollama = ollama;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest req)
    {
        var question = req?.Question ?? string.Empty;
        var isHealthcare = _classifier.IsHealthcareRelated(question);
        if (!isHealthcare)
        {
            var respNon = new ChatResponse("This is outside healthcare scope.", false);
            return Ok(respNon);
        }

        // For healthcare questions, call Ollama via the injected client.
        try
        {
            var gen = await _ollama.GenerateAsync(question);
            var answer = gen ?? $"(Healthcare) No response for: {question}";
            var resp = new ChatResponse(answer, true);
            return Ok(resp);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new ChatResponse($"Error calling Ollama: {ex.Message}", true));
        }
    }
}
