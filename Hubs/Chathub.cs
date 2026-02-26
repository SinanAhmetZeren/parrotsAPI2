using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParrotsAPI2.Models;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ParrotsAPI2.Helpers;
using System.Collections.Concurrent;


public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationPageTracker _tracker;
    private static readonly ConcurrentDictionary<string, string> _userConnections = new();
    private static readonly ConcurrentDictionary<string, bool> _unreadCache = new();

    public ChatHub(ILogger<ChatHub> logger, IServiceScopeFactory scopeFactory, ConversationPageTracker tracker)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _tracker = tracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString() ?? string.Empty;

        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections[userId] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
        await Clients.Caller.SendAsync("ParrotsChatHubInitialized");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Find the user by connection ID in our RAM dictionary
        var entry = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);

        if (!string.IsNullOrEmpty(entry.Key))
        {
            _userConnections.TryRemove(entry.Key, out _);
            _tracker.LeaveMessagesScreen(entry.Key);
            _tracker.LeaveConversation(entry.Key);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string senderId, string receiverId, string content)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var sender = await dbContext.Users.FindAsync(senderId);
        var receiver = await dbContext.Users.FindAsync(receiverId);
        if (sender == null || receiver == null) return;

        // Encrypt message
        var encryptedForSender = EncryptionHelper.EncryptString(content, EncryptionHelper.KeyFromBase64(sender.EncryptionKey));
        var encryptedForReceiver = EncryptionHelper.EncryptString(content, EncryptionHelper.KeyFromBase64(receiver.EncryptionKey));

        var conversationKey = string.CompareOrdinal(senderId, receiverId) < 0
            ? senderId + "_" + receiverId
            : receiverId + "_" + senderId;


        // Create message object
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            TextSenderEncrypted = encryptedForSender,
            TextReceiverEncrypted = encryptedForReceiver,
            DateTime = DateTime.UtcNow,
            Rendered = false,
            ReadByReceiver = false,
            ConversationKey = conversationKey
        };


        // Add message first
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(); // <-- message.Id now generated

        // Find or create conversation
        var conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(c => c.ConversationKey == conversationKey);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                User1Id = senderId,
                User2Id = receiverId,
                ConversationKey = conversationKey
            };
            dbContext.Conversations.Add(conversation);
        }

        // Update conversation with last message info
        conversation.LastMessageId = message.Id;
        conversation.LastMessageDate = message.DateTime;

        await dbContext.SaveChangesAsync();

        // Update receiver notifications
        bool isReceiverViewingChat = _tracker.IsViewingConversation(receiverId, senderId);
        bool isReceiverOnMessagesScreen = _tracker.IsOnMessagesScreen(receiverId);

        if (!isReceiverViewingChat && !isReceiverOnMessagesScreen)
        {
            receiver.UnseenMessages = true;
            if (_userConnections.TryGetValue(receiverId, out var connId))
            {
                await Clients.Client(connId).SendAsync("ReceiveUnreadNotification");
            }
        }

        // Notify receiver
        if (_userConnections.TryGetValue(receiverId, out var receiverConnId))
        {
            await Clients.Client(receiverConnId).SendAsync(
                "ReceiveMessage",
                senderId,
                content,
                message.DateTime,
                sender.ProfileImageUrl ?? string.Empty,
                sender.UserName ?? string.Empty
            );

            await Clients.Client(receiverConnId).SendAsync("ReceiveMessageRefetch");
        }
    }


    public async Task MarkMessagesAsSeen(string userId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var user = await dbContext.Users.FindAsync(userId);
            if (user != null && user.UnseenMessages == true)
            {
                user.UnseenMessages = false;
                try
                {
                    await dbContext.SaveChangesAsync();
                    // _logger.LogInformation($"Database Updated: UnseenMessages set to false for {userId}");
                }
                catch (Exception ex)
                {
                    // _logger.LogError(ex, "Error saving UnseenMessages state for {UserId}", userId);
                }
            }
            else
            {
                // _logger.LogInformation($"No DB Write needed: {userId} had no unseen messages.");
            }
        }
    }

    public async Task EnterConversationPage(string userId, string partnerId)
    {
        _tracker.EnterConversation(userId, partnerId);
        await MarkMessagesAsSeen(userId); // DB write only when actually needed
    }

    public async Task LeaveConversationPage(string userId)
    {
        _tracker.LeaveConversation(userId);
    }

    public async Task EnterMessagesScreen(string userId)
    {
        _tracker.EnterMessagesScreen(userId);
        await MarkMessagesAsSeen(userId);
    }

    public async Task LeaveMessagesScreen(string userId)
    {
        _tracker.LeaveMessagesScreen(userId);
    }
}