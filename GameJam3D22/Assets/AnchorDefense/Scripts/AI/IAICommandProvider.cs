using System.Threading;
using System.Threading.Tasks;

namespace AnchorDefense
{
    public interface IAICommandProvider
    {
        bool IsConfigured { get; }
        Task<AIProviderResult> RequestAsync(AICommandRequest request, CancellationToken cancellationToken);
    }
}
