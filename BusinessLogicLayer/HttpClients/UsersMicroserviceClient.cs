using BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.HttpClients;

public class UsersMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _distributedCache;

    private static readonly UserDTO _fallbackUser = new UserDTO(
        PersonName: "Temporarily Unavailable",
        Email: "Temporarily Unavailable",
        Gender: "Temporarily Unavailable",
        UserID: Guid.Empty);

    public UsersMicroserviceClient(HttpClient httpClient, IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _distributedCache = distributedCache;
    }

    public async Task<UserDTO?> GetUserByID(Guid userID)
    {
        try
        {
            string cacheKey = $"user_{userID}";
            string? cachedUser = await _distributedCache.GetStringAsync(cacheKey);
            if (cachedUser != null)
            {
                return System.Text.Json.JsonSerializer.Deserialize<UserDTO>(cachedUser);
            }

            HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/users/{userID}");

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
                    return _fallbackUser;
                }
            }

            UserDTO? user = await response.Content.ReadFromJsonAsync<UserDTO>();

            if (user == null)
            {
                throw new ArgumentException("Invalid user data");
            }

            // Cache the data for 5 minutes
            await _distributedCache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(user), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(3)
            });

            return user;
        }
        catch (BrokenCircuitException)
        {
            return _fallbackUser;
        }
        catch (TimeoutRejectedException)
        {
            return _fallbackUser;
        }
        catch (TimeoutException)
        {
            return _fallbackUser;
        }
        catch (HttpRequestException ex) when (ex.StatusCode != System.Net.HttpStatusCode.BadRequest)
        {
            return _fallbackUser;
        }
        catch (TaskCanceledException)
        {
            return _fallbackUser;
        }
    }
}
