using ParrotsAPI2.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ParrotsAPI2.Models
{

    public class VehicleImage
    {
        public int Id { get; set; }
        public string VehicleImagePath { get; set; } = string.Empty;

        public int VehicleId { get; set; }

        [JsonIgnore]
        public Vehicle? Vehicle { get; set; }
        public string UserId { get; set; } = string.Empty;
        
    }
    
}
