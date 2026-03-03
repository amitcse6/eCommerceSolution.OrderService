using BusinessLogicLayer.DTO;
using Polly.CircuitBreaker;
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

    private static readonly UserDTO _fallbackUser = new UserDTO(
        PersonName: "Temporarily Unavailable",
        Email: "Temporarily Unavailable",
        Gender: "Temporarily Unavailable",
        UserID: Guid.Empty);

    public UsersMicroserviceClient(HttpClient httpClient)
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
                    return _fallbackUser;
                }
            }

            UserDTO? user = await response.Content.ReadFromJsonAsync<UserDTO>();

            if (user == null)
            {
                throw new ArgumentException("Invalid user data");
            }

            return user;
        }
        catch (BrokenCircuitException)
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
