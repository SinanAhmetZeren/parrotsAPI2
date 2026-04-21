using Microsoft.Extensions.Logging;
using Moq;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.WaypointDtos;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Blob;
using ParrotsAPI2.Services.Waypoint;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class WaypointServiceTests
{
    private WaypointService CreateService(DataContext context)
    {
        var mapper = TestDbContextFactory.CreateMapper();
        var logger = new Mock<ILogger<WaypointService>>().Object;
        var blob = new Mock<IBlobService>().Object;
        return new WaypointService(mapper, context, logger, blob);
    }

    // --- AddWaypointNoImage ---

    [Fact]
    public async Task AddWaypointNoImage_NullDto_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.AddWaypointNoImage(null!, "u1");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AddWaypointNoImage_ValidData_CreatesWaypoint()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var dto = new AddWaypointDto { VoyageId = 1, Latitude = 41.0, Longitude = 29.0, Title = "Stop 1", Order = 1 };
        var result = await service.AddWaypointNoImage(dto, "u1");

        Assert.True(result.Success);
        Assert.Equal(1, context.Waypoints.Count());
        var wp = context.Waypoints.First();
        Assert.Equal("u1", wp.UserId);
        Assert.Equal(string.Empty, wp.ProfileImage);
    }

    // --- DeleteWaypoint ---

    [Fact]
    public async Task DeleteWaypoint_NotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.DeleteWaypoint(999);

        Assert.False(result.Success);
        Assert.Contains("999", result.Message);
    }

    [Fact]
    public async Task DeleteWaypoint_Exists_RemovesWaypoint()
    {
        var context = TestDbContextFactory.Create();
        context.Waypoints.Add(new Waypoint { Id = 1, VoyageId = 1, Order = 2, UserId = "u1" });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.DeleteWaypoint(1);

        Assert.True(result.Success);
        Assert.Equal(0, context.Waypoints.Count());
    }

    [Fact]
    public async Task DeleteWaypoint_Order1_PromotesNextWaypointToOrder1()
    {
        var context = TestDbContextFactory.Create();
        context.Waypoints.Add(new Waypoint { Id = 1, VoyageId = 1, Order = 1, UserId = "u1" });
        context.Waypoints.Add(new Waypoint { Id = 2, VoyageId = 1, Order = 2, UserId = "u1" });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.DeleteWaypoint(1);

        context.ChangeTracker.Clear();
        var remaining = context.Waypoints.Find(2);
        Assert.Equal(1, remaining!.Order);
    }

    [Fact]
    public async Task DeleteWaypoint_NonOrder1_DoesNotChangeOtherOrders()
    {
        var context = TestDbContextFactory.Create();
        context.Waypoints.Add(new Waypoint { Id = 1, VoyageId = 1, Order = 1, UserId = "u1" });
        context.Waypoints.Add(new Waypoint { Id = 2, VoyageId = 1, Order = 2, UserId = "u1" });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.DeleteWaypoint(2); // delete order 2, not order 1

        context.ChangeTracker.Clear();
        var remaining = context.Waypoints.Find(1);
        Assert.Equal(1, remaining!.Order); // order 1 stays unchanged
    }
}
