using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using System.Net.Http; // Ensure this is present
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Policies;

public class ProductMicroservicePolicy : IProductMicroservicePolicy
{
    readonly ILogger<ProductMicroservicePolicy> _logger;

    public ProductMicroservicePolicy(ILogger<ProductMicroservicePolicy> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
    {
        AsyncFallbackPolicy<HttpResponseMessage> policy = Policy<HttpResponseMessage>
            .HandleResult(r => !r.IsSuccessStatusCode)
            .FallbackAsync(
                fallbackAction: (delegateResult, context, cancellationToken) =>
                {
                    var product = new ProductDTO(
                        Guid.Empty,
                        "Unavailable",
                        "N/A",
                        0m,
                        0,
                        0m
                    );
                    
                    HttpResponseMessage fallbackResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(product), 
                            encoding: System.Text.Encoding.UTF8, 
                            mediaType: "application/json")
                    };
                    
                    return Task.FromResult(fallbackResponse);
                },
                onFallbackAsync: (delegateResult, context) =>
                {
                    _logger.LogWarning(
                        "Fallback executed for Product Microservice. Original Status Code: {StatusCode}",
                        delegateResult.Result?.StatusCode);
                    
                    return Task.CompletedTask;
                }
            );

        return policy;
    }
}
