using Microsoft.Extensions.Logging;
using Moq;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.GroupDtos;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Group;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class GroupServiceTests
{
    private GroupService CreateService(DataContext context)
    {
        var logger = new Mock<ILogger<GroupService>>().Object;
        var pushLogger = new Mock<ILogger<ParrotsAPI2.Services.Notifications.ExpoPushService>>().Object;
        var expoPush = new ParrotsAPI2.Services.Notifications.ExpoPushService(new HttpClient(), pushLogger);
        return new GroupService(context, logger, expoPush);
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

    // --- CreateGroup ---

    [Fact]
    public async Task CreateGroup_ValidData_CreatesGroupAndAddsCreatorAsMember()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateGroup(new CreateGroupDto { Name = "My Group", CreatorId = "creator1" });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("My Group", result.Data!.Name);
        Assert.Equal(1, context.GroupConversations.Count());
        Assert.Single(result.Data.Members);
        Assert.Equal("creator1", result.Data.Members[0].UserId);
    }

    [Fact]
    public async Task CreateGroup_GeneratesEncryptionKey()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.CreateGroup(new CreateGroupDto { Name = "Secure Group", CreatorId = "creator1" });

        var group = context.GroupConversations.First();
        Assert.NotNull(group.EncryptionKey);
        Assert.NotEmpty(group.EncryptionKey);
    }

    // --- AddMember ---

    [Fact]
    public async Task AddMember_ByCreator_AddsMember()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        var result = await service.AddMember(group.Data!.Id, "user2", "creator1");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Members.Count);
    }

    [Fact]
    public async Task AddMember_ByNonCreator_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        context.Users.Add(MakeUser("user3", "carol"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        var result = await service.AddMember(group.Data!.Id, "user3", "user2");

        Assert.False(result.Success);
        Assert.Contains("creator", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddMember_AlreadyMember_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        await service.AddMember(group.Data!.Id, "user2", "creator1");
        var result = await service.AddMember(group.Data.Id, "user2", "creator1");

        Assert.False(result.Success);
        Assert.Contains("already", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddMember_GroupNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.AddMember(9999, "user2", "creator1");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- RemoveMember ---

    [Fact]
    public async Task RemoveMember_ByCreator_RemovesMember()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        await service.AddMember(group.Data!.Id, "user2", "creator1");
        var result = await service.RemoveMember(group.Data.Id, "user2", "creator1");

        Assert.True(result.Success);
        Assert.Single(result.Data!.Members);
    }

    [Fact]
    public async Task RemoveMember_ByNonCreator_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        await service.AddMember(group.Data!.Id, "user2", "creator1");
        var result = await service.RemoveMember(group.Data.Id, "creator1", "user2");

        Assert.False(result.Success);
        Assert.Contains("creator", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveMember_NotAMember_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        var result = await service.RemoveMember(group.Data!.Id, "nonexistent", "creator1");

        Assert.False(result.Success);
        Assert.Contains("not a member", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- ExitGroup ---

    [Fact]
    public async Task ExitGroup_AsMember_RemovesSelf()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        context.Users.Add(MakeUser("user2", "bob"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        await service.AddMember(group.Data!.Id, "user2", "creator1");
        var result = await service.ExitGroup(group.Data.Id, "user2");

        Assert.True(result.Success);
        Assert.Single(result.Data!.Members);
    }

    [Fact]
    public async Task ExitGroup_NotMember_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        var result = await service.ExitGroup(group.Data!.Id, "nonexistent");

        Assert.False(result.Success);
        Assert.Contains("not a member", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- GetGroupById ---

    [Fact]
    public async Task GetGroupById_AsMember_ReturnsGroup()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        var result = await service.GetGroupById(group.Data!.Id, "creator1");

        Assert.True(result.Success);
        Assert.Equal("G", result.Data!.Name);
    }

    [Fact]
    public async Task GetGroupById_AsNonMember_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        var result = await service.GetGroupById(group.Data!.Id, "outsider");

        Assert.False(result.Success);
        Assert.Contains("not a member", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- GetGroupMessages ---

    [Fact]
    public async Task GetGroupMessages_AsMember_ReturnsMessages()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        var result = await service.GetGroupMessages(group.Data!.Id, "creator1");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetGroupMessages_AsNonMember_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("creator1", "alice"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var group = await service.CreateGroup(new CreateGroupDto { Name = "G", CreatorId = "creator1" });
        var result = await service.GetGroupMessages(group.Data!.Id, "outsider");

        Assert.False(result.Success);
        Assert.Contains("denied", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
