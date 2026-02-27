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
            // Only initialize if not already present
            if (!_unreadCache.ContainsKey(userId))
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var user = await dbContext.Users.FindAsync(userId);

                // Initialize in-memory unread state from DB
                _unreadCache[userId] = user?.UnseenMessages ?? false;
            }
        }
        await base.OnConnectedAsync();
        await Clients.Caller.SendAsync("ParrotsChatHubInitialized");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Find the user by connection ID in our RAM dictionary
        var entry = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
        var userId = entry.Key;
        if (!string.IsNullOrEmpty(userId))
        {
            // Remove connection
            _userConnections.TryRemove(userId, out _);
            // Leave tracking pages
            _tracker.LeaveMessagesScreen(userId);
            _tracker.LeaveConversation(userId);
            // Persist unread status to DB if it exists in cache
            if (_unreadCache.TryGetValue(userId, out bool hasUnread))
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var user = await dbContext.Users.FindAsync(userId);

                if (user != null)
                {
                    user.UnseenMessages = hasUnread;
                    await dbContext.SaveChangesAsync();
                }

                // Remove from in-memory cache
                _unreadCache.TryRemove(userId, out _);
            }
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
        bool isReceiverOnline = _userConnections.ContainsKey(receiverId);

        //   mark UNREAD if user is NOT actively viewing 
        if (!(isReceiverViewingChat || isReceiverOnMessagesScreen))
        {
            if (isReceiverOnline)
            {
                // User is online → keep in memory & send "ReceiveUnreadNotification"
                _unreadCache[receiverId] = true;
                if (_userConnections.TryGetValue(receiverId, out var connectId))
                {
                    await Clients.Client(connectId).SendAsync("ReceiveUnreadNotification");
                }
            }
            else
            {
                // User is offline → persist immediately
                receiver.UnseenMessages = true;
                await dbContext.SaveChangesAsync();
            }
        }

        // Notify receiver
        if (_userConnections.TryGetValue(receiverId, out var receiverConnId))
        {
            await Clients.Client(receiverConnId).SendAsync("ReceiveMessage",
                senderId,
                content,
                message.DateTime,
                sender.ProfileImageUrl ?? string.Empty,
                sender.UserName ?? string.Empty
            );

            await Clients.Client(receiverConnId).SendAsync("ReceiveMessageRefetch");
        }
    }

    public async Task EnterConversationPage(string userId, string partnerId)
    {
        _tracker.EnterConversation(userId, partnerId);
    }

    public async Task LeaveConversationPage(string userId)
    {
        _tracker.LeaveConversation(userId);
    }

    public async Task EnterMessagesScreen(string userId)
    {
        _tracker.EnterMessagesScreen(userId);
        // Clear in-memory unread state
        _unreadCache[userId] = false;
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var user = await dbContext.Users.FindAsync(userId);
        if (user?.UnseenMessages == true)
        {
            user.UnseenMessages = false;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task LeaveMessagesScreen(string userId)
    {
        _tracker.LeaveMessagesScreen(userId);
    }

    public async Task<bool> CheckUnreadMessages(string userId)
    {
        _unreadCache.TryGetValue(userId, out var isUnread);
        return await Task.FromResult(isUnread);
    }

}




/*
start connection:
{ "protocol":"json","version":1}

WriteMessageToDb:
{ "arguments":["1","2","hello from signalR"],"invocationId":"0","target":"WriteMessageToDb","type":1}

two arguments:
{ "arguments":["arg1!","arg2!!"],"invocationId":"0","target":"SendMessage2","type":1}
*/