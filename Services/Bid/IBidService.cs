using ParrotsAPI2.Dtos.BidDtos;

namespace ParrotsAPI2.Services.Bid
{
    public interface IBidService
    {

        Task<ServiceResponse<GetBidDto>> GetBidById(int bidId);
        Task<ServiceResponse<List<GetBidDto>>> GetBidsByVoyageId(int voyageId);
        Task<ServiceResponse<List<GetBidDto>>> GetBidsByUserId(string userId);
        Task<ServiceResponse<GetBidDto>> CreateBid(BidDto newBid);
        Task<ServiceResponse<GetBidDto>> ChangeBid(ChangeBidDto changedBid);
        Task<ServiceResponse<string>> AcceptBid(int bidId, string voyageOwnerId);
        Task<ServiceResponse<string>> DeleteBid(int bidId, string voyageOwnerId);
    }
}
