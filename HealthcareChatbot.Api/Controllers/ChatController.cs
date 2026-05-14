using Microsoft.AspNetCore.Mvc;
using HealthcareChatbot.Api.Models;
using HealthcareChatbot.Api.Services;

namespace HealthcareChatbot.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IOllamaClient _ollama;

    public ChatController(IOllamaClient ollama)
    {
        _ollama = ollama;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest req)
    {
        var question = req?.Question ?? string.Empty;

        // Forward every question to Ollama; Ollama will classify and respond with JSON.

        try
        {
            var (isHealthcare, answer) = await _ollama.GenerateAsync(question);
            var finalAnswer = answer ?? $"No response for: {question}";
            // Return plain text containing only the assistant's answer.
            return Content(finalAnswer, "text/plain");
        }
        catch (Exception ex)
        {
            return StatusCode(502, new ChatResponse($"Error calling Ollama: {ex.Message}", true));
        }
    }
}