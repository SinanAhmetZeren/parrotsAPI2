using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.VehicleDtos;
using ParrotsAPI2.Dtos.VehicleImageDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Vehicle
{
    public class VehicleService : IVehicleService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public VehicleService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ServiceResponse<List<GetVehicleDto>>> AddVehicle(AddVehicleDto newVehicle)
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();

            if (newVehicle.ImageFile != null && newVehicle.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newVehicle.ImageFile.FileName);
                var filePath = Path.Combine("Uploads/VehicleImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newVehicle.ImageFile.CopyToAsync(stream);
                }
                newVehicle.ProfileImageUrl = "/Uploads/VehicleImages/" + fileName;
            }

            var vehicle = _mapper.Map<Models.Vehicle>(newVehicle);
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var updatedVehicles = await _context.Vehicles.ToListAsync();
            serviceResponse.Data = updatedVehicles.Select(c => _mapper.Map<GetVehicleDto>(c)).ToList();

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> AddVehicleImage(int vehicleId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            var existingVehicle = await _context.Vehicles
                .Include(v => v.VehicleImages)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);
            if (existingVehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not found";
                return serviceResponse;
            }
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine("Uploads/VehicleImages/", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            var newVehicleImage = new VehicleImage
            {
                VehicleImagePath = "/Uploads/VehicleImages/" + fileName
            };
            existingVehicle.VehicleImages ??= new List<VehicleImage>();
            existingVehicle.VehicleImages.Add(newVehicleImage);
            await _context.SaveChangesAsync();
            serviceResponse.Data = _mapper.Map<GetVehicleDto>(existingVehicle);

            return serviceResponse;
        }
    
        public async Task<ServiceResponse<List<GetVehicleDto>>> DeleteVehicle(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    throw new Exception($"Vehicle with ID `{id}` not found");
                }
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
                var vehicles = await _context.Vehicles.ToListAsync();
                serviceResponse.Data = vehicles.Select(c => _mapper.Map<GetVehicleDto>(c)).ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVehicleDto>>> GetAllVehicles()
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();
            var dbVehicles = await _context.Vehicles.ToListAsync();
            serviceResponse.Data = dbVehicles.Select(c => _mapper.Map<GetVehicleDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> GetVehicleById(int id)
        {

            var serviceResponse = new ServiceResponse<GetVehicleDto>();

            var vehicle = await _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.VehicleImages)
                .Include(v => v.Voyages)
                .FirstOrDefaultAsync(c => c.Id == id);

            var userDto = _mapper.Map<UserDto>(vehicle?.User);
            var vehicleImageDtos = _mapper.Map<List<VehicleImageDto>>(vehicle?.VehicleImages);
            var voyageDtos = _mapper.Map<List<VoyageDto>>(vehicle?.Voyages);

            var vehicleDto = _mapper.Map<GetVehicleDto>(vehicle);

            vehicleDto.User = userDto;
            vehicleDto.VehicleImages = vehicleImageDtos;
            vehicleDto.Voyages = voyageDtos;

            serviceResponse.Data = vehicleDto;

            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not found";
                return serviceResponse;
            }
            serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            return serviceResponse;

        }

        public async Task<ServiceResponse<List<GetVehicleDto>>> GetVehiclesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();
            var vehicles = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .ToListAsync();
            serviceResponse.Data = _mapper.Map<List<GetVehicleDto>>(vehicles);
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> PatchVehicle(int vehicleId, [FromBody] JsonPatchDocument<UpdateVehicleDto> patchDoc, ModelStateDictionary modelState)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(vehicleId);
                if (vehicle == null)
                {
                    throw new Exception($"vehicle with ID `{vehicleId}` not found");
                }
                var vehicleDto = _mapper.Map<UpdateVehicleDto>(vehicle);
                patchDoc.ApplyTo(vehicleDto, modelState);
                if (!modelState.IsValid)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid model state after patch operations";
                    return serviceResponse;
                }
                _mapper.Map(vehicleDto, vehicle);
                _context.Vehicles.Attach(vehicle);
                _context.Entry(vehicle).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> UpdateVehicle(UpdateVehicleDto updatedVehicle)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(updatedVehicle.Id);
                if (vehicle == null)
                {
                    throw new Exception($"Vehicle with ID `{updatedVehicle.Id}` not found");
                }
                vehicle.Name = updatedVehicle.Name;
                vehicle.Type = updatedVehicle.Type;
                vehicle.Capacity = updatedVehicle.Capacity;
                vehicle.Description = updatedVehicle.Description;
                vehicle.ProfileImageUrl = updatedVehicle.ProfileImageUrl;

        await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> UpdateVehicleProfileImage(int vehicleId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "vehicle not found";
                return serviceResponse;
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine("Uploads/VehicleImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                vehicle.ProfileImageUrl = "/Uploads/VehicleImages/" + fileName;
                await _context.SaveChangesAsync();
                var vehicleDto = _mapper.Map<GetVehicleDto>(vehicle);
                serviceResponse.Success = true;
                serviceResponse.Data = vehicleDto;
            }
            else
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No image provided";
            }
            return serviceResponse;
        }
    }
}
