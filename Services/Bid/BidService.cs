using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Bid
{
    public class BidService : IBidService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public BidService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
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
                bidEntity.Currency = changedBid.Currency;
                bidEntity.DateTime = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetBidDto>(bidEntity);
            }
            catch (Exception ex)
            {
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
                    PersonCount = newBid.PersonCount,
                    Message = newBid.Message,
                    OfferPrice = newBid.OfferPrice,
                    Currency = newBid.Currency,
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
                    Currency = bidEntity.Currency,
                    DateTime = bidEntity.DateTime,
                    VoyageId = bidEntity.VoyageId,
                    UserId = bidEntity.UserId
                };
                response.Data = createdBidDto;
                response.Message = "Bid created successfully";
            }
            catch (Exception ex)
            {
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
                    .Include(b => b.Voyage)
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(c => c.Id == bidId);

                if (bidEntity != null)
                {
                    var bidDto = new GetBidDto
                    {
                        Id = bidEntity.Id,
                        PersonCount = bidEntity.PersonCount,
                        Message = bidEntity.Message,
                        OfferPrice = bidEntity.OfferPrice,
                        Currency = bidEntity.Currency,
                        DateTime = bidEntity.DateTime,
                        VoyageId = bidEntity.VoyageId,
                        UserId = bidEntity.UserId,
                        VoyageImageUrl = bidEntity.Voyage?.ProfileImage,
                        UserImageUrl = bidEntity.User?.ProfileImageUrl,
                        UserName = bidEntity.User?.UserName,
                        VoyageName = bidEntity.Voyage?.Name
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
                    .Where(b => b.UserId == userId)
                    .Join(
                        _context.Voyages,
                        bid => bid.VoyageId,
                        voyage => voyage.Id,
                        (bid, voyage) => new { Bid = bid, Voyage = voyage }
                    )
                    .Join(
                        _context.Users,
                        combined => combined.Bid.UserId,
                        user => user.Id,
                        (combined, user) => new { Bid = combined.Bid, Voyage = combined.Voyage, User = user }
                    )
                    .Select(combined => new GetBidDto
                    {
                        Id = combined.Bid.Id,
                        PersonCount = combined.Bid.PersonCount,
                        Message = combined.Bid.Message,
                        OfferPrice = combined.Bid.OfferPrice,
                        Currency = combined.Bid.Currency,
                        DateTime = combined.Bid.DateTime,
                        VoyageId = combined.Bid.VoyageId,
                        UserId = combined.Bid.UserId,
                        VoyageImageUrl = combined.Voyage.ProfileImage,
                        UserImageUrl = combined.User.ProfileImageUrl,
                        UserName = combined.User.UserName,
                        VoyageName = combined.Voyage.Name
                    })
                    .ToListAsync();





                response.Data = bids;
                response.Message = "Bids retrieved successfully";
            }
            catch (Exception ex)
            {
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
                var bids2 = await _context.Bids
                    .Where(b => b.VoyageId == voyageId)
                    .Select(b => new GetBidDto
                    {   Id = b.Id,
                        PersonCount = b.PersonCount,
                        Message = b.Message,
                        OfferPrice = b.OfferPrice,
                        Currency = b.Currency,
                        DateTime = b.DateTime,
                        VoyageId = b.VoyageId,
                        UserId = b.UserId
                    })
                    .ToListAsync();


                var bids = await _context.Bids
                    .Where(b => b.VoyageId == voyageId)
                    .Join(
                        _context.Voyages,
                        bid => bid.VoyageId,
                        voyage => voyage.Id,
                        (bid, voyage) => new { Bid = bid, Voyage = voyage }
                    )
                    .Join(
                        _context.Users,
                        combined => combined.Bid.UserId,
                        user => user.Id,
                        (combined, user) => new { Bid = combined.Bid, Voyage = combined.Voyage, User = user }
                    )
                    .Select(combined => new GetBidDto
                    {
                        Id = combined.Bid.Id,
                        PersonCount = combined.Bid.PersonCount,
                        Message = combined.Bid.Message,
                        OfferPrice = combined.Bid.OfferPrice,
                        Currency = combined.Bid.Currency,
                        DateTime = combined.Bid.DateTime,
                        VoyageId = combined.Bid.VoyageId,
                        UserId = combined.Bid.UserId,
                        VoyageImageUrl = combined.Voyage.ProfileImage,
                        UserImageUrl = combined.User.ProfileImageUrl,
                        UserName = combined.User.UserName,
                        VoyageName = combined.Voyage.Name
                    })
                    .ToListAsync();


                response.Data = bids;
                response.Message = "Bids retrieved successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving bids: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<string>> DeleteBid(int bidId)
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
                _context.Bids.Remove(bid);
                await _context.SaveChangesAsync();
                serviceResponse.Data = "Bid successfully deleted";
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error deleting bid: {ex.Message}";
            }
            return serviceResponse;
        }
    }
}

/*
public int VoyageId { get; set; }
public string VoyageName { get; set; }
public string VoyageImageUrl { get; set; }
public string UserId { get; set; }
public string UserName { get; set; }
public string UserImageUrl { get; set; }
*/