using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.RegisterLoginDtos;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class AccountAdditionalTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    public AccountAdditionalTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- accept-terms ---

    [Fact]
    public async Task AcceptTerms_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/Account/accept-terms", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AcceptTerms_WithToken_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Account/accept-terms", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- acknowledge-public-profile ---

    [Fact]
    public async Task AcknowledgePublicProfile_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/Account/acknowledge-public-profile", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AcknowledgePublicProfile_WithToken_Returns200()
    {
        var (token, _) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var response = await authedClient.PostAsync("/api/Account/acknowledge-public-profile", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- sendCode ---

    [Fact]
    public async Task SendCode_UnknownEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/Account/sendCode/nobody@test.com", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendCode_KnownConfirmedUser_Returns200()
    {
        var email = $"sendcode_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });
        _factory.ConfirmUser(email);

        var response = await _client.PostAsync($"/api/Account/sendCode/{email}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- confirmCode ---

    [Fact]
    public async Task ConfirmCode_WrongCode_ReturnsBadRequest()
    {
        var email = $"confirmcode_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });

        var response = await _client.PostAsJsonAsync("/api/Account/confirmCode",
            new { Email = email, Code = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmCode_CorrectCode_Returns200()
    {
        var email = $"confirmok_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });

        // Get the confirmation code directly from DB
        var code = GetConfirmationCode(email);
        Assert.NotNull(code);

        var response = await _client.PostAsJsonAsync("/api/Account/confirmCode",
            new { Email = email, Code = code });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- resetPassword ---

    [Fact]
    public async Task ResetPassword_WrongCode_ReturnsValidationProblem()
    {
        var email = $"resetpw_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });
        _factory.ConfirmUser(email);

        var response = await _client.PostAsJsonAsync("/api/Account/resetPassword",
            new { Email = email, ConfirmationCode = "wrongcode", Password = "NewPass123!" });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_CorrectCode_Returns200()
    {
        var email = $"resetpwok_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });
        _factory.ConfirmUser(email);

        // Trigger sendCode to set a confirmation code
        await _client.PostAsync($"/api/Account/sendCode/{email}", null);
        var code = GetConfirmationCode(email);
        Assert.NotNull(code);

        var response = await _client.PostAsJsonAsync("/api/Account/resetPassword",
            new { Email = email, ConfirmationCode = code, Password = "NewPass123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- refresh-token ---

    [Fact]
    public async Task RefreshToken_InvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/Account/refresh-token",
            new { RefreshToken = "invalid-token-xyz" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_Returns200()
    {
        var email = $"refresh_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/Account/register", new RegisterDto
        {
            Email = email, Password = "Test123!", UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10), TermsVersion = "2026-01"
        });
        _factory.ConfirmUser(email);
        var loginResponse = await _client.PostAsJsonAsync("/api/Account/login",
            new LoginDto { Email = email, Password = "Test123!" });
        var userData = await ApiTestHelper.DeserializeAsync<UserResponseDto>(loginResponse);

        var response = await _client.PostAsJsonAsync("/api/Account/refresh-token",
            new { RefreshToken = userData!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Helpers ---

    private string? GetConfirmationCode(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var normalizedEmail = email.ToUpperInvariant();
        return db.Users.AsNoTracking().FirstOrDefault(u => u.NormalizedEmail == normalizedEmail)?.ConfirmationCode;
    }
}
