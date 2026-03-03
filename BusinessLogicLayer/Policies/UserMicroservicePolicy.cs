using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Net;

namespace BusinessLogicLayer.Policies;

public class UserMicroservicePolicy : IUserMicroservicePolicy
{
    private readonly ILogger<UserMicroservicePolicy> _logger;

    public UserMicroservicePolicy(ILogger<UserMicroservicePolicy> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        var circuitBreakerPolicy = Polly.Policy
            .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromSeconds(60),
                onBreak: (outcome, timespan) =>
                {
                    _logger.LogWarning(
                        "Circuit breaker opened for User Microservice for {Delay}s. Status Code: {StatusCode}",
                        timespan.TotalSeconds,
                        outcome.Result?.StatusCode);
                },
                onReset: () => _logger.LogInformation("Circuit breaker reset for User Microservice.")
            );
        return circuitBreakerPolicy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Polly.Policy
            .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning(
                        "Retry attempt {RetryAttempt} for User Microservice after {Delay}s. Status Code: {StatusCode}",
                        retryAttempt,
                        timespan.TotalSeconds,
                        outcome.Result?.StatusCode);
                }
            );
        return retryPolicy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        AsyncTimeoutPolicy<HttpResponseMessage> policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500));
        return policy;
    }
}
