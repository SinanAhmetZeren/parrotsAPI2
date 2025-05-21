using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Services.Bid;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers


{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

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
            var voyageOwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (voyageOwnerId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }
            if (newBid.UserId != voyageOwnerId)
            {
                return Forbid(); 
            }

            var serviceResponse = await _bidService.CreateBid(newBid);

            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }
            return BadRequest(serviceResponse);
        }


        [HttpPost("acceptbid")]
        public async Task<ActionResult<ServiceResponse<string>>> AcceptBid(int bidId)
        {
            var voyageOwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (voyageOwnerId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }
            var serviceResponse = await _bidService.AcceptBid(bidId, voyageOwnerId);
            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }
            return BadRequest(serviceResponse);
        }



        [HttpPost("changeBid")]
        public async Task<ActionResult<ServiceResponse<BidDto>>> ChangeBid(ChangeBidDto changedBid)
        {

            var voyageOwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (voyageOwnerId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }
 
            var existingBidResponse = await _bidService.GetBidById(changedBid.Id);
            if (!existingBidResponse.Success || existingBidResponse.Data == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Bid not found."
                });
            }

            if (existingBidResponse.Data.UserId != voyageOwnerId)
            {
                return Forbid();
            }

            var serviceResponse = await _bidService.ChangeBid(changedBid);
            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }
            return NotFound(serviceResponse);
        }

        [HttpDelete("deletebid")]
        public async Task<ActionResult<ServiceResponse<string>>> DeleteBid(int bidId)
        {
            var voyageOwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (voyageOwnerId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }
            var serviceResponse = await _bidService.DeleteBid(bidId, voyageOwnerId);
            if (serviceResponse.Success)
            {
                return Ok(serviceResponse);
            }
            return NotFound(serviceResponse);
        }


        /*
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
        */

        /*
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
        */

        /*
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
        */




    }
}