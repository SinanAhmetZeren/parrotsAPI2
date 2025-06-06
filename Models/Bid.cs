﻿using ParrotsAPI2.Models;
using System.Reflection;

namespace ParrotsAPI2.Models
{
    public class Bid
    {
        public int Id { get; set; }
        public int PersonCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal OfferPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public bool Accepted { get; set; }
        public int VoyageId { get; set; }
        public Voyage? Voyage { get; set; }
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

    }
}
