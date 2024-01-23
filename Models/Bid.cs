using ParrotsAPI2.Models;
using System.Reflection;

namespace ParrotsAPI2.Models
{
    public class Bid
    {
        public int Id { get; set; }
        public int PersonCount { get; set; }
        public string Message { get; set; }
        public decimal OfferPrice { get; set; }
        public string Currency { get; set; }
        public DateTime DateTime { get; set; }



        public int VoyageId { get; set; }
        public Voyage Voyage { get; set; }


        public int UserId { get; set; }
        public User User { get; set; }

    }
}
