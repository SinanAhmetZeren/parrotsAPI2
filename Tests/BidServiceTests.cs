using Microsoft.Extensions.Logging;
using Moq;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Bid;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class BidServiceTests
{
    private BidService CreateService(DataContext context)
    {
        var mapper = TestDbContextFactory.CreateMapper();
        var logger = new Mock<ILogger<BidService>>().Object;
        return new BidService(mapper, context, logger);
    }

    // --- CreateBid ---

    [Fact]
    public async Task CreateBid_ValidData_ReturnsBidWithSuccessTrue()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var dto = new BidDto
        {
            PersonCount = 2,
            Message = "Hello",
            OfferPrice = 100,
            DateTime = DateTime.UtcNow,
            VoyageId = 1,
            UserId = "user1"
        };

        var result = await service.CreateBid(dto);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, context.Bids.Count());
    }

    // --- ChangeBid ---

    [Fact]
    public async Task ChangeBid_BidNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var dto = new ChangeBidDto { Id = 999, PersonCount = 1, Message = "test", OfferPrice = 50 };
        var result = await service.ChangeBid(dto);

        Assert.False(result.Success);
        Assert.Contains("999", result.Message);
    }

    [Fact]
    public async Task ChangeBid_ExistingBid_UpdatesFields()
    {
        var context = TestDbContextFactory.Create();
        var bid = new Bid { Id = 1, PersonCount = 1, Message = "old", OfferPrice = 50, VoyageId = 1, UserId = "u1" };
        context.Bids.Add(bid);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new ChangeBidDto { Id = 1, PersonCount = 3, Message = "new message", OfferPrice = 200 };
        var result = await service.ChangeBid(dto);

        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.PersonCount);
        Assert.Equal(200, result.Data.OfferPrice);
    }

    // --- AcceptBid ---

    [Fact]
    public async Task AcceptBid_BidNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.AcceptBid(999, "owner1");

        Assert.False(result.Success);
        Assert.Contains("999", result.Message);
    }

    [Fact]
    public async Task AcceptBid_NotVoyageOwner_ReturnsUnauthorized()
    {
        var context = TestDbContextFactory.Create();
        var voyage = new Voyage { Id = 1, UserId = "owner1" };
        var bid = new Bid { Id = 1, VoyageId = 1, UserId = "bidder1" };
        context.Voyages.Add(voyage);
        context.Bids.Add(bid);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AcceptBid(1, "notowner");

        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.Message);
    }

    [Fact]
    public async Task AcceptBid_ValidOwner_SetsBidAcceptedAndTimestamp()
    {
        var context = TestDbContextFactory.Create();
        var voyage = new Voyage { Id = 1, UserId = "owner1" };
        var bid = new Bid { Id = 1, VoyageId = 1, UserId = "bidder1", Accepted = false };
        context.Voyages.Add(voyage);
        context.Bids.Add(bid);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AcceptBid(1, "owner1");

        Assert.True(result.Success);
        var updatedBid = context.Bids.First();
        Assert.True(updatedBid.Accepted);
        Assert.NotNull(updatedBid.AcceptedAt);
    }

    // --- DeleteBid ---

    [Fact]
    public async Task DeleteBid_NotVoyageOwner_ReturnsUnauthorized()
    {
        var context = TestDbContextFactory.Create();
        var voyage = new Voyage { Id = 1, UserId = "owner1" };
        var bid = new Bid { Id = 1, VoyageId = 1, UserId = "bidder1" };
        context.Voyages.Add(voyage);
        context.Bids.Add(bid);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.DeleteBid(1, "notowner");

        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.Message);
        Assert.Equal(1, context.Bids.Count());
    }

    [Fact]
    public async Task DeleteBid_ValidOwner_RemovesBid()
    {
        var context = TestDbContextFactory.Create();
        var voyage = new Voyage { Id = 1, UserId = "owner1" };
        var bid = new Bid { Id = 1, VoyageId = 1, UserId = "bidder1" };
        context.Voyages.Add(voyage);
        context.Bids.Add(bid);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.DeleteBid(1, "owner1");

        Assert.True(result.Success);
        Assert.Equal(0, context.Bids.Count());
    }
}
