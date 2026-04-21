using System.Net;
using System.Net.Http.Json;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

/// <summary>
/// Verifies that SQL injection attempts in string inputs are handled safely.
/// EF Core uses parameterized queries, so these should never cause errors or leak data.
/// </summary>
public class SecurityTests : IClassFixture<ParrotsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ParrotsWebApplicationFactory _factory;

    private static readonly string[] SqlPayloads =
    [
        "' OR '1'='1",
        "'; DROP TABLE Users; --",
        "' UNION SELECT * FROM Users --",
        "1; SELECT * FROM Users",
        "' OR 1=1 --",
        "admin'--",
        "' OR 'x'='x",
    ];

    public SecurityTests(ParrotsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- Register endpoint ---

    [Theory]
    [MemberData(nameof(GetSqlPayloads))]
    public async Task Register_SqlInjectionInEmail_DoesNotCrash(string payload)
    {
        var response = await _client.PostAsJsonAsync("/api/Account/register", new
        {
            Email = payload,
            Password = "Test123!",
            UserName = $"u_{Guid.NewGuid():N}".Substring(0, 10),
            TermsVersion = "2026-01"
        });

        // Should return 400 (validation) or 200, never 500
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(GetSqlPayloads))]
    public async Task Register_SqlInjectionInUsername_DoesNotCrash(string payload)
    {
        var response = await _client.PostAsJsonAsync("/api/Account/register", new
        {
            Email = $"test_{Guid.NewGuid()}@test.com",
            Password = "Test123!",
            UserName = payload,
            TermsVersion = "2026-01"
        });

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // --- Login endpoint ---

    [Theory]
    [MemberData(nameof(GetSqlPayloads))]
    public async Task Login_SqlInjectionInEmail_DoesNotCrash(string payload)
    {
        var response = await _client.PostAsJsonAsync("/api/Account/login", new
        {
            Email = payload,
            Password = "Test123!"
        });

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(GetSqlPayloads))]
    public async Task Login_SqlInjectionInPassword_DoesNotCrash(string payload)
    {
        var response = await _client.PostAsJsonAsync("/api/Account/login", new
        {
            Email = "legit@test.com",
            Password = payload
        });

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // --- Search endpoint ---

    [Theory]
    [MemberData(nameof(GetSqlPayloads))]
    public async Task SearchUsers_SqlInjectionInQuery_DoesNotCrash(string payload)
    {
        var response = await _client.GetAsync($"/api/User/SearchUsers?query={Uri.EscapeDataString(payload)}");

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // --- Authenticated endpoints ---

    [Theory]
    [MemberData(nameof(GetSqlPayloads))]
    public async Task AddVehicle_SqlInjectionInName_DoesNotCrash(string payload)
    {
        var (token, userId) = await ApiTestHelper.CreateConfirmedUserAsync(_client, _factory);
        var authedClient = _factory.CreateClient();
        ApiTestHelper.SetBearer(authedClient, token);

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(payload), "Name");
        form.Add(new StringContent("Boat"), "Type");
        form.Add(new StringContent("5"), "Capacity");
        form.Add(new StringContent(payload), "Description");
        form.Add(new StringContent(userId), "UserId");

        var response = await authedClient.PostAsync("/api/Vehicle/addVehicle", form);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(GetSqlPayloads))]
    public async Task SendCode_SqlInjectionInEmail_DoesNotCrash(string payload)
    {
        var response = await _client.PostAsJsonAsync("/api/Account/sendCode", new { Email = payload });

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    public static IEnumerable<object[]> GetSqlPayloads() =>
        SqlPayloads.Select(p => new object[] { p });
}
