using Microsoft.Extensions.Logging;
using Moq;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.VehicleDtos;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Blob;
using ParrotsAPI2.Services.Vehicle;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class VehicleServiceTests
{
    private VehicleService CreateService(DataContext context)
    {
        var mapper = TestDbContextFactory.CreateMapper();
        var logger = new Mock<ILogger<VehicleService>>().Object;
        var blob = new Mock<IBlobService>().Object;
        return new VehicleService(mapper, context, logger, blob);
    }

    // --- AddVehicle ---

    [Fact]
    public async Task AddVehicle_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var dto = new AddVehicleDto { UserId = "nonexistent", Name = "Boat", Type = VehicleType.Boat, Capacity = 5 };
        var result = await service.AddVehicle(dto, "nonexistent");

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Message);
    }

    [Fact]
    public async Task AddVehicle_ValidData_CreatesVehicle()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1" });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new AddVehicleDto { UserId = "u1", Name = "My Sailboat", Type = VehicleType.Boat, Capacity = 6 };
        var result = await service.AddVehicle(dto, "u1");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("My Sailboat", result.Data!.Name);
        Assert.Equal(1, context.Vehicles.Count());
    }

    // --- DeleteVehicle ---

    [Fact]
    public async Task DeleteVehicle_NotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.DeleteVehicle(999);

        Assert.False(result.Success);
        Assert.Contains("999", result.Message);
    }

    [Fact]
    public async Task DeleteVehicle_SoftDeletesVehicleAndRelatedVoyages()
    {
        var context = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Id = 1, Name = "Boat", IsDeleted = false };
        var voyage = new Voyage { Id = 1, VehicleId = 1, IsDeleted = false };
        context.Vehicles.Add(vehicle);
        context.Voyages.Add(voyage);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.DeleteVehicle(1);

        Assert.True(result.Success);
        context.ChangeTracker.Clear();
        Assert.True(context.Vehicles.Find(1)!.IsDeleted);
        Assert.True(context.Voyages.Find(1)!.IsDeleted);
    }

    [Fact]
    public async Task DeleteVehicle_RemovesVehicleFavorites()
    {
        var context = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Id = 1, Name = "Boat" };
        var favorite = new Favorite { UserId = "u1", Type = "vehicle", ItemId = 1 };
        context.Vehicles.Add(vehicle);
        context.Favorites.Add(favorite);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.DeleteVehicle(1);

        Assert.Equal(0, context.Favorites.Count());
    }

    // --- CheckAndDeleteVehicle ---

    [Fact]
    public async Task CheckAndDeleteVehicle_HasImages_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Id = 1, Name = "Boat" };
        var image = new VehicleImage { VehicleId = 1, UserId = "u1", VehicleImagePath = "path/img.jpg" };
        context.Vehicles.Add(vehicle);
        context.VehicleImages.Add(image);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.CheckAndDeleteVehicle(1);

        Assert.False(result.Success);
        Assert.Contains("images", result.Message);
    }

    [Fact]
    public async Task CheckAndDeleteVehicle_NoImages_SoftDeletes()
    {
        var context = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Id = 1, Name = "Boat", IsDeleted = false };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.CheckAndDeleteVehicle(1);

        Assert.True(result.Success);
        context.ChangeTracker.Clear();
        Assert.True(context.Vehicles.Find(1)!.IsDeleted);
    }

    // --- ConfirmVehicle ---

    [Fact]
    public async Task ConfirmVehicle_NotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.ConfirmVehicle(999);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ConfirmVehicle_SetsConfirmedTrue()
    {
        var context = TestDbContextFactory.Create();
        context.Vehicles.Add(new Vehicle { Id = 1, Name = "Boat", Confirmed = false });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ConfirmVehicle(1);

        Assert.True(result.Success);
        context.ChangeTracker.Clear();
        Assert.True(context.Vehicles.Find(1)!.Confirmed);
    }
}
