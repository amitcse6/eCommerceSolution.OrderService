using BusinessLogicLayer.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.HttpClients;

public class UserMicroserviceClient
{
    private readonly HttpClient _httpClient;

    public UserMicroserviceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserDTO?> GetUserByID(Guid userID)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"/api/users/{userID}");

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

            UserDTO? user = await response.Content.ReadFromJsonAsync<UserDTO>();

            if (user == null)
            {
                throw new ArgumentException("Invalid user data");
            }

            return user;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Failed to connect to User Microservice at {_httpClient.BaseAddress}. Ensure the service is running and accessible. Details: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HttpRequestException($"Request to User Microservice timed out at {_httpClient.BaseAddress}. The service may be unavailable.", ex);
        }
    }
}
