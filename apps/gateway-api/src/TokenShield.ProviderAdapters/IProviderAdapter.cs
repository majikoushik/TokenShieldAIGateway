using System.Threading;
using System.Threading.Tasks;

namespace TokenShield.ProviderAdapters;

public interface IProviderAdapter
{
    string ProviderName { get; }
    Task<ModelResponse> CompleteChatAsync(ModelRequest request, CancellationToken cancellationToken);
}
