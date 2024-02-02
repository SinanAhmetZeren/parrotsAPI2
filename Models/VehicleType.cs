using System.Text.Json.Serialization;

namespace ParrotsAPI2.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]

    public enum VehicleType
    {
            Boat = 0,
            Car = 1,
            Caravan = 2,
            Bus= 3,
            TinyHouse = 4,
            Walk = 5,
            Motorcycle = 6,
    }
}
