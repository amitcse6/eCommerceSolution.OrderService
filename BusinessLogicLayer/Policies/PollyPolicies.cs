using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using System.Net;

namespace BusinessLogicLayer.Policies;

public class PollyPolicies : IPollyPolicies
{
    private readonly ILogger<PollyPolicies> _logger;

    public PollyPolicies(ILogger<PollyPolicies> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak)
    {
        var circuitBreakerPolicy = Polly.Policy
            .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking,
                durationOfBreak: durationOfBreak,
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

    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Polly.Policy
            .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: retryCount,
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

    public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeSpan)
    {
        AsyncTimeoutPolicy<HttpResponseMessage> policy = Policy.TimeoutAsync<HttpResponseMessage>(timeSpan);
        return policy;
    }
}
