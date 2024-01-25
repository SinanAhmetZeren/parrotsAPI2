﻿using ParrotsAPI2.Dtos.VehicleImageDtos;

namespace ParrotsAPI2.Dtos.VehicleDtos
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }
        public int UserId { get; set; }

    }
}