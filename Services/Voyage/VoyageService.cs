using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Controllers;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Dtos.VehicleImageDtos;
using ParrotsAPI2.Dtos.VoyageImageDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Voyage
{
    public class VoyageService : IVoyageService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public VoyageService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ServiceResponse<List<GetVoyageDto>>> AddVoyage(AddVoyageDto newVoyage)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            if (newVoyage.ImageFile != null && newVoyage.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newVoyage.ImageFile.FileName);
                var filePath = Path.Combine("Uploads/VoyageImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newVoyage.ImageFile.CopyToAsync(stream);
                }
                newVoyage.ProfileImage = "/Uploads/VoyageImages/" + fileName;
            }
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(c => c.Id == newVoyage.VehicleId);
            var voyage = _mapper.Map<Models.Voyage>(newVoyage);
            voyage.VehicleImage = vehicle.ProfileImageUrl;
            _context.Voyages.Add(voyage);
            await _context.SaveChangesAsync();
            var updatedVoyages = await _context.Voyages.ToListAsync();
            serviceResponse.Data = updatedVoyages.Select(c => _mapper.Map<GetVoyageDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> AddVoyageImage(int voyageId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            var existingVoyage = await _context.Voyages
                .Include(v => v.VoyageImages)
                .FirstOrDefaultAsync(v => v.Id == voyageId);
            if (existingVoyage == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Voyage not found";
                return serviceResponse;
            }
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine("Uploads/VoyageImages/", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            var newVoyageImage = new VoyageImage
            {
                VoyageImagePath = "/Uploads/VoyageImages/" + fileName
            };
            existingVoyage.VoyageImages ??= new List<VoyageImage>();
            existingVoyage.VoyageImages.Add(newVoyageImage);
            await _context.SaveChangesAsync();
            serviceResponse.Data = _mapper.Map<GetVoyageDto>(existingVoyage);

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> DeleteVoyage(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            try
            {
                var voyage = await _context.Voyages.FindAsync(id);
                if (voyage == null)
                {
                    throw new Exception($"Voyage with ID `{id}` not found");
                }
                _context.Voyages.Remove(voyage);
                await _context.SaveChangesAsync();
                var voyages = await _context.Voyages.ToListAsync();
                serviceResponse.Data = voyages.Select(c => _mapper.Map<GetVoyageDto>(c)).ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetAllVoyages()
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            var dbVoyages = await _context.Voyages.ToListAsync();
            serviceResponse.Data = dbVoyages.Select(c => _mapper.Map<GetVoyageDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> GetVoyageById(int id)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();

            var voyage = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                .Include(v => v.Bids)
                .FirstOrDefaultAsync(c => c.Id == id);

            var userDto = _mapper.Map<UserDto>(voyage?.User);
            var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage?.VoyageImages);
            var vehicleDtos = _mapper.Map<VehicleDto>(voyage?.Vehicle);
            var bidDtos = _mapper.Map <List<BidDto>>(voyage?.Bids);    

            var voyageDto = _mapper.Map<GetVoyageDto>(voyage);

            voyageDto.User = userDto;
            voyageDto.VoyageImages = voyageImageDtos;
            voyageDto.Vehicle = vehicleDtos;
            voyageDto.Bids = bidDtos;

            serviceResponse.Data = voyageDto;

            if (voyage == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Voyage not found";
                return serviceResponse;
            }
            serviceResponse.Data = _mapper.Map<GetVoyageDto>(voyage);
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            var voyages = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                .Include(v => v.Bids)
                .Where(v => v.UserId == userId)
                .ToListAsync();

            if (voyages == null || voyages.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No voyages found for the given user ID";
                return serviceResponse;
            }

            var voyageDtos = voyages.Select(voyage =>
            {
                var userDto = _mapper.Map<UserDto>(voyage.User);
                var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage.VoyageImages);
                var vehicleDto = _mapper.Map<VehicleDto>(voyage.Vehicle);
                var bidDtos = _mapper.Map<List<BidDto>>(voyage.Bids);

                var voyageDto = _mapper.Map<GetVoyageDto>(voyage);
                voyageDto.User = userDto;
                voyageDto.VoyageImages = voyageImageDtos;
                voyageDto.Vehicle = vehicleDto;
                voyageDto.Bids = bidDtos;

                return voyageDto;
            }).ToList();

            serviceResponse.Data = voyageDtos;
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByVehicleId(int vehicleId)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();

            var voyages = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                .Include(v => v.Bids)
                .Where(v => v.VehicleId == vehicleId)
                .ToListAsync();

            if (voyages == null || voyages.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No voyages found for the given vehicle ID";
                return serviceResponse;
            }

            var voyageDtos = voyages.Select(voyage =>
            {
                var userDto = _mapper.Map<UserDto>(voyage.User);
                var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage.VoyageImages);
                var vehicleDto = _mapper.Map<VehicleDto>(voyage.Vehicle);
                var bidDtos = _mapper.Map<List<BidDto>>(voyage.Bids);

                var voyageDto = _mapper.Map<GetVoyageDto>(voyage);
                voyageDto.User = userDto;
                voyageDto.VoyageImages = voyageImageDtos;
                voyageDto.Vehicle = vehicleDto;
                voyageDto.Bids = bidDtos;

                return voyageDto;
            }).ToList();

            serviceResponse.Data = voyageDtos;
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> PatchVoyage(int voyageId, JsonPatchDocument<UpdateVoyageDto> patchDoc, ModelStateDictionary modelState)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            try
            {
                var voyage = await _context.Voyages.FindAsync(voyageId);
                if (voyage == null)
                {
                    throw new Exception($"Voyage with ID `{voyageId}` not found");
                }

                var voyageDto = _mapper.Map<UpdateVoyageDto>(voyage);
                patchDoc.ApplyTo(voyageDto, modelState);

                if (!modelState.IsValid)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid model state after patch operations";
                    return serviceResponse;
                }

                _mapper.Map(voyageDto, voyage);
                _context.Voyages.Attach(voyage);
                _context.Entry(voyage).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                serviceResponse.Data = _mapper.Map<GetVoyageDto>(voyage);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> UpdateVoyage(UpdateVoyageDto updatedVoyage)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            try
            {
                var voyage = await _context.Voyages.FindAsync(updatedVoyage.Id);
                if (voyage == null)
                {
                    throw new Exception($"Vehicle with ID `{updatedVoyage.Id}` not found");
                }
                voyage.Name = updatedVoyage.Name;
                voyage.Brief = updatedVoyage.Brief;
                voyage.Description = updatedVoyage.Description;
                voyage.Vacancy = updatedVoyage.Vacancy;
                voyage.StartDate = updatedVoyage.StartDate;
                voyage.EndDate = updatedVoyage.EndDate;
                voyage.LastBidDate = updatedVoyage.LastBidDate;
                voyage.MinPrice = updatedVoyage.MinPrice;
                voyage.MaxPrice = updatedVoyage.MaxPrice;
                voyage.FixedPrice = updatedVoyage.FixedPrice;
                voyage.Auction = updatedVoyage.Auction;
                voyage.ProfileImage = updatedVoyage.ProfileImage;


                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetVoyageDto>(voyage);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> UpdateVoyageProfileImage(int voyageId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            try
            {
                var voyage = await _context.Voyages.FindAsync(voyageId);
                if (voyage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Voyage not found";
                    return serviceResponse;
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine("Uploads/VoyageImages/", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    voyage.ProfileImage = "/Uploads/VoyageImages/" + fileName;
                    await _context.SaveChangesAsync();
                    var voyageDto = _mapper.Map<GetVoyageDto>(voyage);
                    serviceResponse.Success = true;
                    serviceResponse.Data = voyageDto;
                }
                else
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No image provided";
                }

                return serviceResponse;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
                return serviceResponse;
            }
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByCoordinates(double lat1, double lat2, double lon1, double lon2)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();

            try
            {
                var voyages = await _context.Voyages
                    .Where(v =>
                        v.Waypoints.Any(w =>
                            w.Order == 1 &&
                            w.Latitude >= lat1 &&
                            w.Latitude <= lat2 &&
                            w.Longitude >= lon1 &&
                            w.Longitude <= lon2))
                    .ToListAsync();

                if (voyages == null || voyages.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No voyages found with waypoints matching the specified conditions.";
                    return serviceResponse;
                }

                var voyageDtos = _mapper.Map<List<GetVoyageDto>>(voyages);
                serviceResponse.Data = voyageDtos;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving voyages: {ex.Message}";
            }

            return serviceResponse;
        }


        public async Task<ServiceResponse<List<int>>> GetVoyageIdsByCoordinates(double lat1, double lat2, double lon1, double lon2)
        {
            var serviceResponse = new ServiceResponse<List<int>>();

            try
            {
                var voyageIds = await _context.Voyages
                    .Where(v =>
                        v.Waypoints.Any(w =>
                            w.Order == 1 &&
                            w.Latitude >= lat1 &&
                            w.Latitude <= lat2 &&
                            w.Longitude >= lon1 &&
                            w.Longitude <= lon2))
                    .Select(v => v.Id)
                    .ToListAsync();

                if (voyageIds == null || voyageIds.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No voyages found with waypoints matching the specified conditions.";
                    return serviceResponse;
                }

                serviceResponse.Data = voyageIds;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving voyage IDs: {ex.Message}";
            }

            return serviceResponse;
        }

    }
}
