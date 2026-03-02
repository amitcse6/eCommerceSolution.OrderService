
using Polly;

namespace BusinessLogicLayer.Policy;

public interface IUserMicroservicePolicy
{
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy();
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();
}
