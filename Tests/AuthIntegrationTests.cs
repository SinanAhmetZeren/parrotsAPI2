using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using parrotsAPI2.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.RegisterLoginDtos;
using ParrotsAPI2.Models;

namespace parrotsAPI2.Tests;

public class AuthIntegrationTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public AuthIntegrationTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- Register ---

    [Fact]
    public async Task Register_ValidPayload_Returns200WithToken()
    {
        var payload = new RegisterDto
        {
            Email = $"newuser_{Guid.NewGuid()}@test.com",
            Password = "Test123!",
            UserName = $"user_{Guid.NewGuid():N}".Substring(0, 12),
            TermsVersion = "2026-01"
        };

        var response = await _client.PostAsJsonAsync("/api/Account/register", payload);

        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}: {content}");
        var body = await ApiTestHelper.DeserializeAsync<UserResponseDto>(response);
        Assert.NotNull(body);
        Assert.NotEmpty(body!.Token);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var email = $"dup_{Guid.NewGuid()}@test.com";
        var payload = new RegisterDto { Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01" };

        await _client.PostAsJsonAsync("/api/Account/register", payload);
        _factory.ConfirmUser(email); // confirmed users block duplicate registration
        payload.UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10);
        var response = await _client.PostAsJsonAsync("/api/Account/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Login ---

    [Fact]
    public async Task Login_CorrectCredentials_Returns200WithToken()
    {
        var email = $"login_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });
        _factory.ConfirmUser(email);

        var response = await _client.PostAsJsonAsync("/api/Account/login",
            new LoginDto { Email = email, Password = "Test123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiTestHelper.DeserializeAsync<UserResponseDto>(response);
        Assert.NotNull(body?.Token);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"wrongpass_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });
        _factory.ConfirmUser(email);

        var response = await _client.PostAsJsonAsync("/api/Account/login",
            new LoginDto { Email = email, Password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/Account/login",
            new LoginDto { Email = "nobody@test.com", Password = "Test123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Protected endpoint ---

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/User/getUserById/someuser");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_IsNotRejectedAs401()
    {
        var email = $"token_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });
        _factory.ConfirmUser(email);
        var loginResponse = await _client.PostAsJsonAsync("/api/Account/login",
            new LoginDto { Email = email, Password = "Test123!" });
        var userData = await ApiTestHelper.DeserializeAsync<UserResponseDto>(loginResponse);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userData!.Token);

        var response = await client.GetAsync($"/api/User/getUserById/{userData.UserId}");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class ParrotsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TokenKey"] = "super-secret-test-key-that-is-long-enough-for-hmac-sha512-algorithm-at-least-64-bytes!!",
                ["Google:ClientId"] = "test-client-id",
                ["Google:ClientSecret"] = "test-client-secret",
                ["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")!,
            });
        });
    }

    public new HttpClient CreateClient()
    {
        var client = base.CreateClient();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        db.Database.EnsureCreated();
        if (!db.TermsVersions.Any())
        {
            db.TermsVersions.Add(new TermsVersion { Version = "2026-01", IsCurrent = true });
            db.SaveChanges();
        }

        const string seedUserId = "seed-user-id";
        if (!db.Users.Any(u => u.Id == seedUserId))
        {
            db.Users.Add(new AppUser
            {
                Id = seedUserId,
                UserName = "seeduser",
                NormalizedUserName = "SEEDUSER",
                Email = "seed@test.com",
                NormalizedEmail = "SEED@TEST.COM",
                Confirmed = true,
                EncryptionKey = "seedkey1234567890123456789012345"
            });
            db.SaveChanges();
        }

        if (!db.Vehicles.Any(v => v.Name == "TestBoat"))
        {
            db.Vehicles.Add(new Vehicle
            {
                Name = "TestBoat",
                Type = VehicleType.Boat,
                Capacity = 10,
                Description = "Test vehicle",
                UserId = seedUserId,
                Confirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
            db.SaveChanges();
        }

        return client;
    }

    public void GiveCoins(string userId, int amount)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var user = db.Users.Find(userId);
        if (user != null)
        {
            user.ParrotCoinBalance = amount;
            db.SaveChanges();
        }
    }

    public int GetSeededVehicleId()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return db.Vehicles.First(v => v.Name == "TestBoat").Id;
    }

    public void GiveAdminRole(string userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var user = db.Users.Find(userId);
        if (user != null)
        {
            user.IsAdmin = true;
            db.SaveChanges();
        }
    }

    public void ConfirmUser(string email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var normalizedEmail = email.ToUpperInvariant();
        var user = db.Users.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail);
        if (user != null)
        {
            user.Confirmed = true;
            db.SaveChanges();
        }
    }
}
