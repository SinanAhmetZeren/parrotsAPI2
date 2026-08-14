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

    // --- PurchaseCrackers ---

    [Fact]
    public async Task PurchaseCrackers_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.PurchaseCrackers("nonexistent", 100, 1.0m, "pay_1");

        Assert.False(result.Success);
        Assert.Equal("User not found.", result.Message);
    }

    [Fact]
    public async Task PurchaseCrackers_AddsCoinsAndCreatesRecord()
    {
        var context = TestDbContextFactory.Create();
        var user = new AppUser { Id = "u1", ParrotCrackerBalance = 50 };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.PurchaseCrackers("u1", 100, 1.5m, "pay_abc");

        Assert.True(result.Success);
        Assert.Equal(150, result.Data);
        Assert.Equal(1, context.CrackerPurchases.Count());
        var purchase = context.CrackerPurchases.First();
        Assert.Equal(100, purchase.CrackersAmount);
        Assert.Equal(1.5m, purchase.EurAmount);
        Assert.Equal("pay_abc", purchase.PaymentProviderId);
    }

    // --- ClaimFreeCrackers ---

    [Fact]
    public async Task ClaimFreeCrackers_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.ClaimFreeCrackers("nonexistent");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ClaimFreeCrackers_BalanceAbove500_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1", ParrotCrackerBalance = 200 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ClaimFreeCrackers("u1");

        Assert.False(result.Success);
        Assert.Contains("200", result.Message);
    }

    [Fact]
    public async Task ClaimFreeCrackers_LowBalance_Adds100CoinsAndCreatesRecord()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1", ParrotCrackerBalance = 0 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.ClaimFreeCrackers("u1");

        Assert.True(result.Success);
        Assert.Equal(100, result.Data);
        Assert.Equal(1, context.CrackerPurchases.Count());
        Assert.StartsWith("free_claim_", context.CrackerPurchases.First().PaymentProviderId);
    }

    // --- SendParrotCrackers ---

    [Fact]
    public async Task SendParrotCrackers_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "receiver", ParrotCrackerBalance = 0 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.SendParrotCrackers("nonexistent", "receiver", 10);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SendParrotCrackers_InsufficientBalance_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1", ParrotCrackerBalance = 5 });
        context.Users.Add(new AppUser { Id = "u2", ParrotCrackerBalance = 0 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.SendParrotCrackers("u1", "u2", 100);

        Assert.False(result.Success);
        Assert.Contains("Insufficient", result.Message);
    }

    [Fact]
    public async Task SendParrotCrackers_ValidTransfer_UpdatesBalancesAndCreatesTransactions()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(new AppUser { Id = "u1", UserName = "sender", ParrotCrackerBalance = 200 });
        context.Users.Add(new AppUser { Id = "u2", UserName = "receiver", ParrotCrackerBalance = 0 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.SendParrotCrackers("u1", "u2", 50);

        Assert.True(result.Success);
        Assert.Equal(150, result.Data);

        context.ChangeTracker.Clear();
        Assert.Equal(150, context.Users.Find("u1")!.ParrotCrackerBalance);
        Assert.Equal(50, context.Users.Find("u2")!.ParrotCrackerBalance);
        Assert.Equal(2, context.CrackerTransactions.Count());
    }
}
