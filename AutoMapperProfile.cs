﻿using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Dtos.FavoriteDtos;
using ParrotsAPI2.Dtos.MessageDtos;
using ParrotsAPI2.Dtos.VehicleDtos;
using ParrotsAPI2.Dtos.VehicleImageDtos;
using ParrotsAPI2.Dtos.VoyageDtos;
using ParrotsAPI2.Dtos.VoyageImageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;

namespace ParrotsAPI2
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            
            CreateMap<AppUser, GetUserDto>();
            CreateMap<AddUserDto, AppUser>();
            CreateMap<AppUser, UpdateUserDto>();
            CreateMap<UpdateUserDto, AppUser>();

            CreateMap<Vehicle, GetVehicleDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
            CreateMap<AppUser, UserDto>();


            CreateMap<Vehicle, GetVehicleDto>();
            CreateMap<AddVehicleDto, Vehicle>();
            CreateMap<Vehicle, UpdateVehicleDto>();
            CreateMap<UpdateVehicleDto, Vehicle>();
            CreateMap<Vehicle, VehicleDto>();
            CreateMap<VehicleDto, Vehicle>();
            CreateMap<GetUsersVehiclesDto, Vehicle>();
            CreateMap<Vehicle, GetUsersVehiclesDto>();
            
            CreateMap<VehicleImage, VehicleImageDto>();
            CreateMap<VehicleImageDto, VehicleImage>();

            CreateMap<VoyageImage, VoyageImageDto>();
            CreateMap<VoyageImageDto, VoyageImage>();

            CreateMap<Voyage, GetVoyageDto>();
            CreateMap<Voyage, VoyageDto>();
            CreateMap<AddVoyageDto, Voyage>();
            CreateMap<Voyage, UpdateVoyageDto>();
            CreateMap<UpdateVoyageDto, Voyage>();
            CreateMap<GetUsersVoyagesDto, Voyage>();
            CreateMap<Voyage, GetUsersVoyagesDto>();

            CreateMap<Voyage, GetVoyageDto>()
                .ForMember(dest => dest.VoyageImages, opt => opt.MapFrom(src => src.VoyageImages));

            CreateMap<Bid, BidDto>();
            CreateMap<BidDto, Bid>();
            CreateMap<Bid, VoyageBidDto>();
            CreateMap<VoyageBidDto, Bid>();

            CreateMap<Bid, GetBidDto>();
            CreateMap<GetBidDto, Bid>();

            CreateMap<Waypoint, GetWaypointDto>();
            CreateMap<AddWaypointDto, Waypoint>();
            CreateMap<Waypoint, WaypointDto>();
            CreateMap<WaypointDto, Waypoint>();

            CreateMap<Message, GetMessageDto>();
            CreateMap<GetMessageDto, Message>();
            CreateMap<Message, AddMessageDto>();
            CreateMap<AddMessageDto, Message>();
            CreateMap<Message, UpdateMessageDto>();
            CreateMap<UpdateMessageDto, Message>();

            CreateMap<Favorite, GetFavoriteDto>();
            CreateMap<GetFavoriteDto, Favorite>();
            CreateMap<Favorite, AddFavoriteDto>();
            CreateMap<AddFavoriteDto, Favorite>();
        }
    }
}
