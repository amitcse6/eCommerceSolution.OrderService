using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Policies;

public interface IPollyPolicies
{
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount);
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        int handledEventsAllowedBeforeBreaking,
        TimeSpan durationOfBreak);
    IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeSpan);
}
