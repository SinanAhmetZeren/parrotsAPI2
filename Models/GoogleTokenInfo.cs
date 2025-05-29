using System.Text.Json.Serialization;

public class GoogleTokenInfo
{
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("verified_email")]
    public bool VerifiedEmail { get; set; } // <- changed name and type
    
    [JsonPropertyName("audience")]
    public string Audience { get; set; }

    [JsonPropertyName("exp")]
    public long Exp { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
