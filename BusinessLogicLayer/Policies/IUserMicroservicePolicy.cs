using Polly;

namespace BusinessLogicLayer.Policies;

public interface IUserMicroservicePolicy
{
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy();
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();
}
