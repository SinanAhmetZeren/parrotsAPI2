﻿namespace ParrotsAPI2.Dtos.WaypointDtos
{
    public class GetWaypointDto
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ProfileImage { get; set; }
        public int Order { get; set; }
        public int VoyageId { get; set; }
    }
}
