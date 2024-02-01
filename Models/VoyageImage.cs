using ParrotsAPI2.Models;
using System.Collections.Generic;

namespace ParrotsAPI2.Models
{

    public class VoyageImage
    {
        public int Id { get; set; }
        public string VoyageImagePath { get; set; }
        public int VoyageId { get; set; }
        public Voyage Voyage { get; set; }
    }
    
}
