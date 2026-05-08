using ParrotsAPI2.Data;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Bookmark;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class BookmarkServiceTests
{
    private BookmarkService CreateService(DataContext context)
    {
        var mapper = TestDbContextFactory.CreateMapper();
        return new BookmarkService(mapper, context);
    }

    private static AppUser MakeUser(string id, string username) => new AppUser
    {
        Id = id,
        UserName = username,
        NormalizedUserName = username.ToUpperInvariant(),
        Email = $"{id}@test.com",
        NormalizedEmail = $"{id}@test.com".ToUpperInvariant(),
        Confirmed = true,
        EncryptionKey = "key1234567890123456789012345678"
    };

    // --- AddBookmark ---

    [Fact]
    public async Task AddBookmark_ValidData_CreatesBookmark()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("user1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.AddBookmark("user1", "user2");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, context.UserBookmarks.Count());
    }

    [Fact]
    public async Task AddBookmark_Self_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("user1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.AddBookmark("user1", "user1");

        Assert.False(result.Success);
        Assert.Contains("yourself", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddBookmark_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("user1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.AddBookmark("user1", "nonexistent");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddBookmark_Duplicate_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("user1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.AddBookmark("user1", "user2");
        var result = await service.AddBookmark("user1", "user2");

        Assert.False(result.Success);
        Assert.Contains("already", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- RemoveBookmark ---

    [Fact]
    public async Task RemoveBookmark_Exists_RemovesRecord()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("user1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.AddBookmark("user1", "user2");
        var result = await service.RemoveBookmark("user1", "user2");

        Assert.True(result.Success);
        Assert.Equal(0, context.UserBookmarks.Count());
    }

    [Fact]
    public async Task RemoveBookmark_NotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.RemoveBookmark("user1", "user2");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- GetBookmarks ---

    [Fact]
    public async Task GetBookmarks_ReturnsBookmarkedUsers()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("user1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.AddBookmark("user1", "user2");
        var result = await service.GetBookmarks("user1");

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal("bob", result.Data![0].UserName);
    }

    [Fact]
    public async Task GetBookmarks_NoBookmarks_ReturnsEmptyList()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.GetBookmarks("user1");

        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    // --- GetBookmarkedUserIds ---

    [Fact]
    public async Task GetBookmarkedUserIds_ReturnsOnlyIds()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("user1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        context.Users.Add(MakeUser("user3", "carol"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.AddBookmark("user1", "user2");
        await service.AddBookmark("user1", "user3");
        var result = await service.GetBookmarkedUserIds("user1");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        Assert.Contains("user2", result.Data);
        Assert.Contains("user3", result.Data);
    }
}
