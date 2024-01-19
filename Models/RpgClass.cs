using System.Text.Json.Serialization;

namespace ParrotsAPI2.Models
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RpgClass
    {
        Knight = 0,
        Mage = 1,
        Cleric = 2,
    }
}
