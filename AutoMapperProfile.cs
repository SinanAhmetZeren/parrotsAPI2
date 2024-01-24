using ParrotsAPI2.Dtos.VehicleDtos;
using ParrotsAPI2.Dtos.VehicleImageDtos;

namespace ParrotsAPI2
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Character, GetCharacterDto>();
            CreateMap<AddCharacterDto, Character>();
            
            CreateMap<User, GetUserDto>();
            CreateMap<AddUserDto, User>();
            CreateMap<User, UpdateUserDto>();
            CreateMap<UpdateUserDto, User>();

            CreateMap<Vehicle, GetVehicleDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
            CreateMap<User, UserDto>();


            CreateMap<Vehicle, GetVehicleDto>();
            CreateMap<AddVehicleDto, Vehicle>();
            CreateMap<Vehicle, UpdateVehicleDto>();
            CreateMap<UpdateVehicleDto, Vehicle>();

            CreateMap<VehicleImage, VehicleImageDto>();
            CreateMap<VehicleImageDto, VehicleImage>();

        }
    }
}
