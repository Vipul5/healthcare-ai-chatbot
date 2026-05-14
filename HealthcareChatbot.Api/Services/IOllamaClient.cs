using System.Threading;
using System.Threading.Tasks;

namespace HealthcareChatbot.Api.Services;

public interface IOllamaClient
{
    Task<(bool IsHealthcare, string Answer)> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
