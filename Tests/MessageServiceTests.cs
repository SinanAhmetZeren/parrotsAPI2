using Microsoft.Extensions.Logging;
using Moq;
using ParrotsAPI2.Data;
using ParrotsAPI2.Helpers;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Message;
using parrotsAPI2.Tests.Helpers;

namespace parrotsAPI2.Tests;

public class MessageServiceTests
{
    private static readonly string TestKeyBase64 = Convert.ToBase64String(new byte[32]); // 32 zero-bytes = valid AES-256 key

    private MessageService CreateService(DataContext context)
    {
        var logger = new Mock<ILogger<MessageService>>().Object;
        return new MessageService(context, logger);
    }

    private static string Encrypt(string plainText) =>
        EncryptionHelper.EncryptString(plainText, EncryptionHelper.KeyFromBase64(TestKeyBase64));

    private static AppUser MakeUser(string id, string username) =>
        new AppUser { Id = id, UserName = username, EncryptionKey = TestKeyBase64 };

    // --- GetMessagesByUserId ---

    [Fact]
    public async Task GetMessagesByUserId_UserNotFound_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.GetMessagesByUserId("nonexistent");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetMessagesByUserId_NoConversations_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.Add(MakeUser("u1", "user1"));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetMessagesByUserId("u1");

        Assert.False(result.Success);
        Assert.Contains("No messages", result.Message);
    }

    [Fact]
    public async Task GetMessagesByUserId_WithConversation_ReturnsDtos()
    {
        var context = TestDbContextFactory.Create();
        var u1 = MakeUser("u1", "user1");
        var u2 = MakeUser("u2", "user2");
        context.Users.AddRange(u1, u2);
        await context.SaveChangesAsync();

        var msg = new Message
        {
            Id = 1,
            SenderId = "u1",
            ReceiverId = "u2",
            TextSenderEncrypted = Encrypt("Hello!"),
            TextReceiverEncrypted = Encrypt("Hello!"),
            DateTime = DateTime.UtcNow
        };
        context.Messages.Add(msg);

        var conversation = new Conversation
        {
            User1Id = "u1",
            User2Id = "u2",
            ConversationKey = "u1_u2",
            LastMessageId = 1,
            LastMessageDate = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetMessagesByUserId("u1");

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal("Hello!", result.Data![0].Text);
    }

    // --- GetMessagesBetweenUsers ---

    [Fact]
    public async Task GetMessagesBetweenUsers_NoMessages_ReturnsFailure()
    {
        var context = TestDbContextFactory.Create();
        context.Users.AddRange(MakeUser("u1", "user1"), MakeUser("u2", "user2"));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetMessagesBetweenUsers("u1", "u2");

        Assert.False(result.Success);
        Assert.Contains("No messages", result.Message);
    }

    [Fact]
    public async Task GetMessagesBetweenUsers_WithMessages_ReturnsDecryptedDtos()
    {
        var context = TestDbContextFactory.Create();
        context.Users.AddRange(MakeUser("u1", "user1"), MakeUser("u2", "user2"));
        await context.SaveChangesAsync();

        context.Messages.Add(new Message
        {
            SenderId = "u1",
            ReceiverId = "u2",
            TextSenderEncrypted = Encrypt("Hi there"),
            TextReceiverEncrypted = Encrypt("Hi there"),
            DateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetMessagesBetweenUsers("u1", "u2");

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal("Hi there", result.Data![0].Text);
    }

    [Fact]
    public async Task GetMessagesBetweenUsers_BothDirections_ReturnsAll()
    {
        var context = TestDbContextFactory.Create();
        context.Users.AddRange(MakeUser("u1", "user1"), MakeUser("u2", "user2"));
        await context.SaveChangesAsync();

        context.Messages.AddRange(
            new Message { SenderId = "u1", ReceiverId = "u2", TextSenderEncrypted = Encrypt("Msg1"), TextReceiverEncrypted = Encrypt("Msg1"), DateTime = DateTime.UtcNow },
            new Message { SenderId = "u2", ReceiverId = "u1", TextSenderEncrypted = Encrypt("Msg2"), TextReceiverEncrypted = Encrypt("Msg2"), DateTime = DateTime.UtcNow.AddSeconds(1) }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetMessagesBetweenUsers("u1", "u2");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }
}
