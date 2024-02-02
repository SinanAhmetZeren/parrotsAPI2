using System.Text.Json.Serialization;

namespace ParrotsAPI2.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]

    public enum VehicleType
    {
            Boat,
            Car,
            Caravan,
            Bus,
            Walk,
            Run, 
            Motorcycle,
            Bicycle,
            TinyHouse,
            Airplane
    }
}
