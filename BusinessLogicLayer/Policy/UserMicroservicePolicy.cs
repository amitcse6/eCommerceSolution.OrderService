using Polly;
using Polly.Retry;
using Microsoft.Extensions.Logging;

namespace BusinessLogicLayer.Policy;

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
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(120),
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

    IAsyncPolicy<HttpResponseMessage> IUserMicroservicePolicy.GetRetryPolicy()
    {
        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Polly.Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
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
}
