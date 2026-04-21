using System.Net.Http.Json;
using Newtonsoft.Json;
using ParrotsAPI2.Dtos.RegisterLoginDtos;

namespace parrotsAPI2.Tests.Helpers;

public static class ApiTestHelper
{
    public static async Task<(string Token, string UserId)> CreateConfirmedUserAsync(
        HttpClient client, ParrotsWebApplicationFactory factory, string? emailPrefix = null)
    {
        var prefix = emailPrefix ?? "testuser";
        var email = $"{prefix}_{Guid.NewGuid()}@test.com";
        var username = $"u_{Guid.NewGuid():N}".Substring(0, 10);

        await client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email,
            Password = "Test123!",
            UserName = username,
            TermsVersion = "2026-01"
        });

        factory.ConfirmUser(email);

        var loginResponse = await client.PostAsJsonAsync("/api/Account/login", new LoginDto
        {
            Email = email,
            Password = "Test123!"
        });

        var userData = await DeserializeAsync<UserResponseDto>(loginResponse);
        return (userData!.Token, userData.UserId);
    }

    public static void SetBearer(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonConvert.DeserializeObject<T>(json);
    }
}
