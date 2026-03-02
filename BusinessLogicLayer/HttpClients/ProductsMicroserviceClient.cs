using BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.HttpClients;

public class ProductsMicroserviceClient
{
    private readonly HttpClient _httpClient;

    public ProductsMicroserviceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductDTO?> GetProductByID(Guid productID)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"/api/products/search/product-id/{productID}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
                }
                else
                {
                    throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
                }
            }

            ProductDTO? product = await response.Content.ReadFromJsonAsync<ProductDTO>();

            if (product == null)
            {
                throw new ArgumentException("Invalid product data");
            }

            return product;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Failed to connect to Product Microservice at {_httpClient.BaseAddress}. Ensure the service is running and accessible. Details: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HttpRequestException($"Request to Product Microservice timed out at {_httpClient.BaseAddress}. The service may be unavailable.", ex);
        }
    }
}
