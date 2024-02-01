using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Services.Bid;

namespace ParrotsAPI2.Controllers


{
    [ApiController]
    [Route("api/[controller]")]
    public class BidController : ControllerBase
    {
        private readonly IBidService _bidService;

        public BidController(IBidService bidService)
        {
            _bidService = bidService;
        }

        [HttpPost("createBid")]
        public async Task<ActionResult<ServiceResponse<BidDto>>> CreateBid(BidDto newBid)
        {
            var serviceResponse = await _bidService.CreateBid(newBid);

            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }

            return BadRequest(serviceResponse);
        }

        [HttpGet("bidId/{bidId}")]
        public async Task<ActionResult<ServiceResponse<BidDto>>> GetBidById(int bidId)
        {
            var serviceResponse = await _bidService.GetBidById(bidId);

            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }

            return NotFound(serviceResponse);
        }

        [HttpPost("changeBid")]
        public async Task<ActionResult<ServiceResponse<BidDto>>> ChangeBid(ChangeBidDto changedBid)
        {
            var serviceResponse = await _bidService.ChangeBid(changedBid);

            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }

            return NotFound(serviceResponse);
        }


        [HttpGet("userBids/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<BidDto>>>> GetBidsByUserId(string userId)
        {
            var serviceResponse = await _bidService.GetBidsByUserId(userId);

            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }

            return NotFound(serviceResponse);
        }

        [HttpGet("voyageBids/{voyageId}")]
        public async Task<ActionResult<ServiceResponse<List<BidDto>>>> GetBidsByVoyageId(int voyageId)
        {
            var serviceResponse = await _bidService.GetBidsByVoyageId(voyageId);

            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }

            return NotFound(serviceResponse);
        }
    }
}