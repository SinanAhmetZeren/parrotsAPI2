using ParrotsAPI2.Models;
using System.Collections.Generic;

namespace ParrotsAPI2.Models
{

    public class VehicleImage
    {
        public int Id { get; set; }
        public string VehicleImagePath { get; set; }

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }
    }
    
}
