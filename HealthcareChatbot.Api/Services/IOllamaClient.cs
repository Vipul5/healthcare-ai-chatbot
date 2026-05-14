using System.Threading;
using System.Threading.Tasks;

namespace HealthcareChatbot.Api.Services;

public interface IOllamaClient
{
    Task<string?> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
