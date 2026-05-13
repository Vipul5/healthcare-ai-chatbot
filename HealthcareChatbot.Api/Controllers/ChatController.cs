using Microsoft.AspNetCore.Mvc;
using HealthcareChatbot.Api.Models;
using HealthcareChatbot.Api.Services;

namespace HealthcareChatbot.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IHealthcareClassifier _classifier;

    public ChatController(IHealthcareClassifier classifier)
    {
        _classifier = classifier;
    }

    [HttpPost]
    public IActionResult Post([FromBody] ChatRequest req)
    {
        var question = req?.Question ?? string.Empty;
        var isHealthcare = _classifier.IsHealthcareRelated(question);
        var answer = isHealthcare ? $"(Healthcare) Mock response to: {question}" : "This is outside healthcare scope.";
        var resp = new ChatResponse(answer, isHealthcare);
        return Ok(resp);
    }
}
