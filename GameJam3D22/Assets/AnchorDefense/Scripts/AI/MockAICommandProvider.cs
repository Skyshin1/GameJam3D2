using System.Threading;
using System.Threading.Tasks;

namespace AnchorDefense
{
    public sealed class MockAICommandProvider : IAICommandProvider
    {
        private readonly AIProviderResult result;

        public MockAICommandProvider(AIParsedCommand command)
        {
            result = AIProviderResult.Ok(command);
        }

        public MockAICommandProvider(AIProviderError error, string message)
        {
            result = AIProviderResult.Fail(error, message);
        }

        public bool IsConfigured => true;
        public int RequestCount { get; private set; }
        public AICommandRequest LastRequest { get; private set; }

        public Task<AIProviderResult> RequestAsync(AICommandRequest request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequest = request;
            return cancellationToken.IsCancellationRequested
                ? Task.FromResult(AIProviderResult.Fail(AIProviderError.Cancelled, "请求已取消"))
                : Task.FromResult(result);
        }
    }
}
