using System.Text.Json.Serialization;

public class GoogleTokenInfo
{
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("email_verified")]
    public string EmailVerified { get; set; }

    [JsonPropertyName("exp")]
    public long Exp { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
