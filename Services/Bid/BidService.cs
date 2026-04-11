using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Bid
{
    public class BidService : IBidService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly ILogger<BidService> _logger;

        public BidService(IMapper mapper, DataContext context, ILogger<BidService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResponse<GetBidDto>> ChangeBid(ChangeBidDto changedBid)
        {
            var serviceResponse = new ServiceResponse<GetBidDto>();
            try
            {
                var bidEntity = await _context.Bids.FirstOrDefaultAsync(c => c.Id == changedBid.Id);
                if (bidEntity == null)
                {
                    throw new Exception($"Bid with ID `{changedBid.Id}` not found");
                }

                bidEntity.PersonCount = changedBid.PersonCount;
                bidEntity.Message = changedBid.Message;
                bidEntity.OfferPrice = changedBid.OfferPrice;
                bidEntity.DateTime = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetBidDto>(bidEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing bid {BidId}", changedBid.Id);
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetBidDto>> CreateBid(BidDto newBid)
        {
            var response = new ServiceResponse<GetBidDto>();
            try
            {
                var bidEntity = new Models.Bid
                {
                    Accepted = false,
                    PersonCount = newBid.PersonCount,
                    Message = newBid.Message,
                    OfferPrice = newBid.OfferPrice,
                    // Currency = newBid.Currency,
                    DateTime = newBid.DateTime,
                    VoyageId = newBid.VoyageId,
                    UserId = newBid.UserId
                };
                _context.Bids.Add(bidEntity);
                await _context.SaveChangesAsync();
                var createdBidDto = new GetBidDto
                {
                    PersonCount = bidEntity.PersonCount,
                    Message = bidEntity.Message,
                    OfferPrice = bidEntity.OfferPrice,
                    // Currency = bidEntity.Currency,
                    DateTime = bidEntity.DateTime,
                    VoyageId = bidEntity.VoyageId,
                    UserId = bidEntity.UserId
                };
                response.Data = createdBidDto;
                response.Message = "Bid created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bid for voyage {VoyageId}", newBid.VoyageId);
                response.Success = false;
                response.Message = "Error creating bid: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<GetBidDto>> GetBidById(int bidId)
        {
            var response = new ServiceResponse<GetBidDto>();
            try
            {
                var bidEntity = await _context.Bids
                    .FirstOrDefaultAsync(c => c.Id == bidId);

                if (bidEntity != null)
                {
                    var bidDto = new GetBidDto
                    {
                        Accepted = bidEntity.Accepted,
                        Id = bidEntity.Id,
                        PersonCount = bidEntity.PersonCount,
                        Message = bidEntity.Message,
                        OfferPrice = bidEntity.OfferPrice,
                        DateTime = bidEntity.DateTime,
                        VoyageId = bidEntity.VoyageId,
                        UserId = bidEntity.UserId
                    };

                    response.Data = bidDto;
                    response.Message = "Bid found successfully";
                }
                else
                {
                    response.Success = false;
                    response.Message = "Bid not found";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bid {BidId}", bidId);
                response.Success = false;
                response.Message = "Error retrieving bid: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<List<GetBidDto>>> GetBidsByUserId(string userId)
        {
            var response = new ServiceResponse<List<GetBidDto>>();
            try
            {
                var bids = await _context.Bids
                    .AsNoTracking()
                    .Where(b => b.UserId == userId)
                    .Select(b => new GetBidDto
                    {
                        Id = b.Id,
                        Accepted = b.Accepted,
                        PersonCount = b.PersonCount,
                        Message = b.Message,
                        OfferPrice = b.OfferPrice,
                        DateTime = b.DateTime,
                        VoyageId = b.VoyageId,
                        UserId = b.UserId
                    })
                    .ToListAsync();

                var userIds = bids.Select(b => b.UserId).Distinct().ToList();
                var userNames = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName })
                    .ToListAsync();
                var userNameMap = userNames.ToDictionary(u => u.Id, u => u.UserName);
                foreach (var bid in bids)
                    bid.UserName = userNameMap.GetValueOrDefault(bid.UserId);

                response.Data = bids;
                response.Message = "Bids retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bids for user {UserId}", userId);
                response.Success = false;
                response.Message = "Error retrieving bids: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<List<GetBidDto>>> GetBidsByVoyageId(int voyageId)
        {
            var response = new ServiceResponse<List<GetBidDto>>();
            try
            {
                var bids = await _context.Bids
                    .AsNoTracking()
                    .Where(b => b.VoyageId == voyageId)
                    .Select(b => new GetBidDto
                    {
                        Id = b.Id,
                        Accepted = b.Accepted,
                        PersonCount = b.PersonCount,
                        Message = b.Message,
                        OfferPrice = b.OfferPrice,
                        DateTime = b.DateTime,
                        VoyageId = b.VoyageId,
                        UserId = b.UserId
                    })
                    .ToListAsync();

                var userIds = bids.Select(b => b.UserId).Distinct().ToList();
                var userNames = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName })
                    .ToListAsync();
                var userNameMap = userNames.ToDictionary(u => u.Id, u => u.UserName);
                foreach (var bid in bids)
                    bid.UserName = userNameMap.GetValueOrDefault(bid.UserId);


                response.Data = bids;
                response.Message = "Bids retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bids for voyage {VoyageId}", voyageId);
                response.Success = false;
                response.Message = "Error retrieving bids: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<string>> DeleteBid(int bidId, string voyageOwnerId)
        {
            var serviceResponse = new ServiceResponse<string>();
            try
            {
                var bid = await _context.Bids.FindAsync(bidId);
                if (bid == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Bid with ID `{bidId}` not found";
                    return serviceResponse;
                }

                var voyage = await _context.Voyages
                        .FirstOrDefaultAsync(v => v.Id == bid.VoyageId);

                if (voyage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Voyage with ID {bid.VoyageId} not found";
                    return serviceResponse;
                }

                if (voyage.UserId != voyageOwnerId)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Unauthorized: You are not the owner of this voyage.";
                    return serviceResponse;
                }

                _context.Bids.Remove(bid);
                await _context.SaveChangesAsync();
                serviceResponse.Success = true;
                serviceResponse.Data = "Bid successfully deleted";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bid {BidId}", bidId);
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error deleting bid: {ex.Message}";
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> AcceptBid(int bidId, string voyageOwnerId)
        {
            var serviceResponse = new ServiceResponse<string>();

            try
            {
                var bidEntity = await _context.Bids.FirstOrDefaultAsync(c => c.Id == bidId);
                if (bidEntity == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Bid with ID {bidId} not found";
                    return serviceResponse;
                }

                var voyage = await _context.Voyages
                        .FirstOrDefaultAsync(v => v.Id == bidEntity.VoyageId);

                if (voyage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Voyage with ID {bidEntity.VoyageId} not found";
                    return serviceResponse;
                }

                if (voyage.UserId != voyageOwnerId)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Unauthorized: You are not the owner of this voyage.";
                    return serviceResponse;
                }

                bidEntity.Accepted = true;
                bidEntity.AcceptedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                serviceResponse.Success = true;
                serviceResponse.Data = "Bid accepted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting bid {BidId}", bidId);
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error accepting bid: {ex.Message}";
            }
            return serviceResponse;
        }



        public async Task<ServiceResponse<GetBidDto>> PatchBid(int bidId, JsonPatchDocument<ChangeBidDto> patchDoc, ModelStateDictionary modelState)
        {
            var serviceResponse = new ServiceResponse<GetBidDto>();

            try
            {
                var bid = await _context.Bids.FindAsync(bidId);
                if (bid == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Bid with ID `{bidId}` not found";
                    return serviceResponse;
                }

                if (patchDoc == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Patch document is null";
                    return serviceResponse;
                }

                var bidDto = _mapper.Map<ChangeBidDto>(bid);
                patchDoc.ApplyTo(bidDto, modelState);

                if (!modelState.IsValid)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid model state after patch operations";
                    return serviceResponse;
                }

                _mapper.Map(bidDto, bid);
                _context.Bids.Update(bid);
                await _context.SaveChangesAsync();

                serviceResponse.Data = _mapper.Map<GetBidDto>(bid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching bid {BidId}", bidId);
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error patching voyage: {ex.Message}";
                if (ex.InnerException != null)
                {
                    serviceResponse.Message += $" Inner Exception: {ex.InnerException.Message}";
                }
            }

            return serviceResponse;
        }



    }
}

