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
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
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

            _userConnections.AddOrUpdate(
                    userId,
                    _ => new HashSet<string> { Context.ConnectionId },
                    (_, existingSet) =>
                    {
                        lock (existingSet)
                        {
                            existingSet.Add(Context.ConnectionId);
                        }
                        return existingSet;
                    });

            // Only initialize unread cache if not present
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
        var connectionId = Context.ConnectionId;
        // Find the user that owns this connection
        var userEntry = _userConnections
            .FirstOrDefault(kvp => kvp.Value.Contains(connectionId));
        var userId = userEntry.Key;
        if (!string.IsNullOrEmpty(userId) &&
            _userConnections.TryGetValue(userId, out var connections))
        {
            bool removeUserCompletely = false;
            lock (connections)
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    removeUserCompletely = true;
                }
            }
            if (removeUserCompletely)
            {
                _userConnections.TryRemove(userId, out _);
                // Persist unread status to DB (only when LAST device disconnects)
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
                    _unreadCache.TryRemove(userId, out _);
                }
            }
        }
        // IMPORTANT: remove tracking only for THIS connection
        _tracker.LeaveMessagesScreen(connectionId);
        _tracker.LeaveConversation(connectionId);
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

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(); // message.Id now generated

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

        if (!(isReceiverViewingChat || isReceiverOnMessagesScreen))
        {
            if (isReceiverOnline)
            {
                // User is online → keep in memory & send "ReceiveUnreadNotification"
                // Mark unread in memory
                _unreadCache[receiverId] = true;

                // Send notification to all receiver connections
                if (_userConnections.TryGetValue(receiverId, out var connectionIds))
                {
                    foreach (var connId in connectionIds)
                    {
                        await Clients.Client(connId).SendAsync("ReceiveUnreadNotification");
                    }
                }
            }
            else
            {
                // User offline → persist unread immediately
                receiver.UnseenMessages = true;
                await dbContext.SaveChangesAsync();
            }
        }

        // Notify all receiver connections of new message
        if (_userConnections.TryGetValue(receiverId, out var receiverConnections))
        {
            foreach (var connId in receiverConnections)
            {
                await Clients.Client(connId).SendAsync("ReceiveMessage",
                    senderId,
                    content,
                    message.DateTime,
                    sender.ProfileImageUrl ?? string.Empty,
                    sender.UserName ?? string.Empty
                );

                await Clients.Client(connId).SendAsync("ReceiveMessageRefetch");
            }
        }
    }


    public Task EnterConversationPage(string userId, string partnerId)
    {
        _tracker.EnterConversation(userId, Context.ConnectionId, partnerId);
        return Task.CompletedTask;
    }

    public Task LeaveConversationPage(string userId)
    {
        _tracker.LeaveConversation(Context.ConnectionId);
        return Task.CompletedTask;
    }

    public async Task EnterMessagesScreen(string userId)
    {
        _tracker.EnterMessagesScreen(userId, Context.ConnectionId); // Pass connectionId
        // Clear in-memory unread state for this user
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

    public Task LeaveMessagesScreen(string userId)
    {
        _tracker.LeaveMessagesScreen(Context.ConnectionId); // Remove using connectionId
        return Task.CompletedTask;
    }

    // public Task<bool> CheckUnreadMessages(string userId)
    // {
    //     var isUnread = _unreadCache.TryGetValue(userId, out var value) && value;
    //     return Task.FromResult(isUnread);
    // }

    public async Task<bool> CheckUnreadMessages(string userId)
    {
        // 1️⃣ Try cache first
        if (_unreadCache.TryGetValue(userId, out var hasUnread))
        {
            return hasUnread;
        }

        // 2️⃣ Cache miss → load from DB
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        var user = await dbContext.Users.FindAsync(userId);

        bool dbValue = user?.UnseenMessages ?? false;

        // 3️⃣ Populate cache
        _unreadCache[userId] = dbValue;

        return dbValue;
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