using Microsoft.Extensions.Logging;
using Moq;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.VoyageDtos;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Blob;
using ParrotsAPI2.Services.Voyage;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class VoyageServiceTests
{
    private VoyageService CreateService(DataContext context)
    {
        var mapper = TestDbContextFactory.CreateMapper();
        var logger = new Mock<ILogger<VoyageService>>().Object;
        var blob = new Mock<IBlobService>().Object;
        return new VoyageService(mapper, context, logger, blob);
    }

    // --- AddVoyage ---

    [Fact]
    public async Task AddVoyage_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var dto = new AddVoyageDto { UserId = "nonexistent", VehicleId = 1, StartDate = DateTime.UtcNow.AddDays(5) };
        var result = await service.AddVoyage(dto, "nonexistent");

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Message);
    }

    [Fact]
    public async Task AddVoyage_VehicleNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var user = new AppUser { Id = "u1", ParrotCoinBalance = 100 };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new AddVoyageDto { UserId = "u1", VehicleId = 999, StartDate = DateTime.UtcNow.AddDays(2) };
        var result = await service.AddVoyage(dto, "u1");

        Assert.False(result.Success);
        Assert.Equal("Vehicle not found.", result.Message);
    }

    [Fact]
    public async Task AddVoyage_NotEnoughCoins_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var user = new AppUser { Id = "u1", ParrotCoinBalance = 0 };
        var vehicle = new Vehicle { Id = 1, Name = "Sailboat" };
        context.Users.Add(user);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new AddVoyageDto { UserId = "u1", VehicleId = 1, StartDate = DateTime.UtcNow.AddDays(10) };
        var result = await service.AddVoyage(dto, "u1");

        Assert.False(result.Success);
        Assert.Equal("Not enough ParrotCoins.", result.Message);
    }

    [Fact]
    public async Task AddVoyage_HappyPath_DeductsCoinsAndCreatesTransaction()
    {
        var context = TestDbContextFactory.Create();
        int daysAhead = 5;
        var user = new AppUser { Id = "u1", ParrotCoinBalance = 100 };
        var vehicle = new Vehicle { Id = 1, Name = "Sailboat" };
        context.Users.Add(user);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var startDate = DateTime.UtcNow.AddDays(daysAhead);
        var dto = new AddVoyageDto { UserId = "u1", VehicleId = 1, StartDate = startDate, Name = "Test Voyage" };
        var result = await service.AddVoyage(dto, "u1");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        var updatedUser = context.Users.First();
        Assert.True(updatedUser.ParrotCoinBalance < 100); // coins were deducted

        Assert.Equal(1, context.CoinTransactions.Count());
        var tx = context.CoinTransactions.First();
        Assert.Equal("voyage_cost", tx.Type);
        Assert.True(tx.Amount <= 0);
    }

    // --- GetFilteredVoyages ---

    [Fact]
    public async Task GetFilteredVoyages_WithinBounds_ReturnsMatchingVoyages()
    {
        var context = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Id = 1, Name = "Boat" };
        var user = new AppUser { Id = "u1" };

        var insideVoyage = new Voyage
        {
            Id = 1, UserId = "u1", Confirmed = true, IsDeleted = false, PublicOnMap = true,
            PlaceType = 0, LastBidDate = DateTime.UtcNow.AddDays(5),
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(10),
            VehicleId = 1,
            Waypoints = new List<Waypoint> { new Waypoint { Order = 1, Latitude = 41.0, Longitude = 29.0, VoyageId = 1 } }
        };

        var outsideVoyage = new Voyage
        {
            Id = 2, UserId = "u1", Confirmed = true, IsDeleted = false, PublicOnMap = true,
            PlaceType = 0, LastBidDate = DateTime.UtcNow.AddDays(5),
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(10),
            VehicleId = 1,
            Waypoints = new List<Waypoint> { new Waypoint { Order = 1, Latitude = 51.0, Longitude = 10.0, VoyageId = 2 } }
        };

        context.Users.Add(user);
        context.Vehicles.Add(vehicle);
        context.Voyages.AddRange(insideVoyage, outsideVoyage);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetFilteredVoyages(
            lat1: 40.0, lat2: 42.0, lon1: 28.0, lon2: 30.0,
            vacancy: null, vehicleType: null, startDate: null, endDate: null);

        Assert.True(result.Success);
        Assert.Single(result.Data!.Where(v => v.Id == 1));
        Assert.DoesNotContain(result.Data, v => v.Id == 2);
    }

    [Fact]
    public async Task GetFilteredVoyages_ExcludesDeletedAndUnconfirmed()
    {
        var context = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Id = 1, Name = "Boat" };
        var user = new AppUser { Id = "u1" };

        var deleted = new Voyage
        {
            Id = 1, UserId = "u1", Confirmed = true, IsDeleted = true, PublicOnMap = true,
            PlaceType = 0, LastBidDate = DateTime.UtcNow.AddDays(5),
            VehicleId = 1,
            Waypoints = new List<Waypoint> { new Waypoint { Order = 1, Latitude = 41.0, Longitude = 29.0, VoyageId = 1 } }
        };

        var unconfirmed = new Voyage
        {
            Id = 2, UserId = "u1", Confirmed = false, IsDeleted = false, PublicOnMap = true,
            PlaceType = 0, LastBidDate = DateTime.UtcNow.AddDays(5),
            VehicleId = 1,
            Waypoints = new List<Waypoint> { new Waypoint { Order = 1, Latitude = 41.0, Longitude = 29.0, VoyageId = 2 } }
        };

        context.Users.Add(user);
        context.Vehicles.Add(vehicle);
        context.Voyages.AddRange(deleted, unconfirmed);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetFilteredVoyages(null, null, null, null, null, null, null, null);

        // Service returns Success=false with "No voyages found" when result is empty — that's correct behavior
        Assert.False(result.Success);
        Assert.Equal("No voyages found matching the filters.", result.Message);
    }
}
