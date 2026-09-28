using System.Text.Json.Serialization;

namespace ParrotsAPI2.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VoyageState
    {
        Active,
        BidsClosed,
        Cancelled
    }
}
