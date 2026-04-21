using Microsoft.Extensions.Logging;
using Moq;
using ParrotsAPI2.Data;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Blob;
using ParrotsAPI2.Services.User;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class UserServiceTests
{
    private UserService CreateService(DataContext context)
    {
        var mapper = TestDbContextFactory.CreateMapper();
        var logger = new Mock<ILogger<UserService>>().Object;
        var blob = new Mock<IBlobService>().Object;
        return new UserService(mapper, context, logger, blob);
    }

    // --- PurchaseCoins ---

    [Fact]
    public async Task PurchaseCoins_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.PurchaseCoins("nonexistent", 100, 1.0m, "pay_1");

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Message);
    }

    [Fact]
    public async Task PurchaseCoins_AddsCoinsAndCreatesRecord()
    {
        var context = TestDbContextFactory.Create();
        var user = new AppUser { Id = "u1", ParrotCoinBalance = 50 };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.PurchaseCoins("u1", 100, 1.5m, "pay_abc");

        Assert.True(result.Success);
        Assert.Equal(150, result.Data);
        Assert.Equal(1, context.CoinPurchases.Count());
        var purchase = context.CoinPurchases.First();
        Assert.Equal(100, purchase.CoinsAmount);
        Assert.Equal(1.5m, purchase.EurAmount);
        Assert.Equal("pay_abc", purchase.PaymentProviderId);
    }

    // --- ClaimFreeCoins ---

    [Fact]
    public async Task ClaimFreeCoins_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.ClaimFreeCoins("nonexistent");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ClaimFreeCoins_BalanceAbove500_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1", ParrotCoinBalance = 500 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ClaimFreeCoins("u1");

        Assert.False(result.Success);
        Assert.Contains("500", result.Message);
    }

    [Fact]
    public async Task ClaimFreeCoins_LowBalance_Adds100CoinsAndCreatesRecord()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1", ParrotCoinBalance = 0 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ClaimFreeCoins("u1");

        Assert.True(result.Success);
        Assert.Equal(100, result.Data);
        Assert.Equal(1, context.CoinPurchases.Count());
        Assert.Equal("free_claim", context.CoinPurchases.First().PaymentProviderId);
    }

    // --- SendParrotCoins ---

    [Fact]
    public async Task SendParrotCoins_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "receiver", ParrotCoinBalance = 0 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.SendParrotCoins("nonexistent", "receiver", 10);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SendParrotCoins_InsufficientBalance_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1", ParrotCoinBalance = 5 });
        context.Users.Add(new AppUser { Id = "u2", ParrotCoinBalance = 0 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.SendParrotCoins("u1", "u2", 100);

        Assert.False(result.Success);
        Assert.Contains("Insufficient", result.Message);
    }

    [Fact]
    public async Task SendParrotCoins_ValidTransfer_UpdatesBalancesAndCreatesTransactions()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1", UserName = "sender", ParrotCoinBalance = 200 });
        context.Users.Add(new AppUser { Id = "u2", UserName = "receiver", ParrotCoinBalance = 0 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.SendParrotCoins("u1", "u2", 50);

        Assert.True(result.Success);
        Assert.Equal(150, result.Data);

        context.ChangeTracker.Clear();
        Assert.Equal(150, context.Users.Find("u1")!.ParrotCoinBalance);
        Assert.Equal(50, context.Users.Find("u2")!.ParrotCoinBalance);
        Assert.Equal(2, context.CoinTransactions.Count());
    }
}
