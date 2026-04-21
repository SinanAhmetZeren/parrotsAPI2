using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.FavoriteDtos;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Message;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class FavoriteServiceTests
{
    private FavoriteService CreateService(DataContext context)
    {
        var mapper = TestDbContextFactory.CreateMapper();
        return new FavoriteService(mapper, context);
    }

    // --- AddFavorite ---

    [Fact]
    public async Task AddFavorite_NullInput_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.AddFavorite(null!);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AddFavorite_MissingUserId_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.AddFavorite(new AddFavoriteDto { UserId = "", Type = "vehicle", ItemId = 1 });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AddFavorite_InvalidItemId_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.AddFavorite(new AddFavoriteDto { UserId = "u1", Type = "vehicle", ItemId = 0 });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AddFavorite_ValidData_CreatesFavorite()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.AddFavorite(new AddFavoriteDto { UserId = "u1", Type = "vehicle", ItemId = 5 });

        Assert.True(result.Success);
        Assert.Equal(1, context.Favorites.Count());
    }

    [Fact]
    public async Task AddFavorite_Duplicate_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Favorites.Add(new Favorite { UserId = "u1", Type = "vehicle", ItemId = 5 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AddFavorite(new AddFavoriteDto { UserId = "u1", Type = "vehicle", ItemId = 5 });

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }

    // --- DeleteFavoriteVehicle ---

    [Fact]
    public async Task DeleteFavoriteVehicle_NotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.DeleteFavoriteVehicle("u1", 99);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteFavoriteVehicle_Exists_RemovesRecord()
    {
        var context = TestDbContextFactory.Create();
        context.Favorites.Add(new Favorite { UserId = "u1", Type = "vehicle", ItemId = 5 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.DeleteFavoriteVehicle("u1", 5);

        Assert.True(result.Success);
        Assert.Equal(0, context.Favorites.Count());
    }

    // --- DeleteFavoriteVoyage ---

    [Fact]
    public async Task DeleteFavoriteVoyage_NotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.DeleteFavoriteVoyage("u1", 99);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteFavoriteVoyage_Exists_RemovesRecord()
    {
        var context = TestDbContextFactory.Create();
        context.Favorites.Add(new Favorite { UserId = "u1", Type = "voyage", ItemId = 3 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.DeleteFavoriteVoyage("u1", 3);

        Assert.True(result.Success);
        Assert.Equal(0, context.Favorites.Count());
    }

    // --- GetFavoriteVehicleIdsByUserId ---

    [Fact]
    public async Task GetFavoriteVehicleIds_ReturnsOnlyVehicleIds()
    {
        var context = TestDbContextFactory.Create();
        context.Favorites.Add(new Favorite { UserId = "u1", Type = "vehicle", ItemId = 10 });
        context.Favorites.Add(new Favorite { UserId = "u1", Type = "vehicle", ItemId = 20 });
        context.Favorites.Add(new Favorite { UserId = "u1", Type = "voyage", ItemId = 30 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetFavoriteVehicleIdsByUserId("u1");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        Assert.Contains(10, result.Data);
        Assert.Contains(20, result.Data);
        Assert.DoesNotContain(30, result.Data);
    }
}
