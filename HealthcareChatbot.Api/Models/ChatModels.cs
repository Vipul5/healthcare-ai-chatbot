namespace HealthcareChatbot.Api.Models;

public record ChatRequest(string Question);
public record ChatResponse(string Answer, bool IsHealthcare);
