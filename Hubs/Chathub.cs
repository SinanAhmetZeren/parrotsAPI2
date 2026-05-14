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
using ParrotsAPI2.Services;


public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationPageTracker _tracker;
    private readonly ExpoPushService _expoPush;
    // userId → set of active SignalR connection IDs (one user can have multiple tabs/devices)
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    // userId → whether the user currently has unread messages (mirrors DB UnseenMessages, avoids repeated DB reads)
    private static readonly ConcurrentDictionary<string, bool> _unreadCache = new();
    private static readonly ConcurrentDictionary<string, List<DateTime>> _messageSendTimestamps = new();
    // userId → EncryptionKey, ProfileImageUrl, UserName (populated on first message, avoids repeated DB lookups)
    private static readonly ConcurrentDictionary<string, CachedUserInfo> _userInfoCache = new();
    private const int MessageRateLimit = 5;
    private static readonly TimeSpan MessageRateWindow = TimeSpan.FromSeconds(5);

    private record CachedUserInfo(string EncryptionKey, string ProfileImageUrl, string UserName);

    public static IReadOnlyDictionary<string, HashSet<string>> GetUserConnections() => _userConnections;
    public static IReadOnlyDictionary<string, bool> GetUnreadCache() => _unreadCache;
    public static IEnumerable<(string UserId, string UserName)> GetUserInfoCache() =>
        _userInfoCache.Select(kvp => (kvp.Key, kvp.Value.UserName));

    public ChatHub(ILogger<ChatHub> logger, IServiceScopeFactory scopeFactory, ConversationPageTracker tracker, ExpoPushService expoPush)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _tracker = tracker;
        _expoPush = expoPush;
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
        string? userId = null;

        // 1️⃣ Find the user that owns this connection
        foreach (var kvp in _userConnections)
        {
            if (kvp.Value.Contains(connectionId))
            {
                userId = kvp.Key;
                break;
            }
        }

        if (!string.IsNullOrEmpty(userId) &&
            _userConnections.TryGetValue(userId, out var connections))
        {
            bool removeUserCompletely = false;

            // 2️⃣ Remove this connection (thread-safe)
            lock (connections)
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    removeUserCompletely = true;
                }
            }

            // 3️⃣ If last connection → persist unread state
            if (removeUserCompletely)
            {
                _userConnections.TryRemove(userId, out _);
                _messageSendTimestamps.TryRemove(userId, out _);
                _userInfoCache.TryRemove(userId, out _);

                if (_unreadCache.TryGetValue(userId, out bool hasUnread))
                {
                    try
                    {
                        await using var scope = _scopeFactory.CreateAsyncScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                        var user = await dbContext.Users.FindAsync(userId);
                        if (user != null)
                        {
                            user.UnseenMessages = hasUnread;
                            await dbContext.SaveChangesAsync();
                        }

                        _unreadCache.TryRemove(userId, out _);
                    }
                    catch (Exception ex)
                    {
                        // Log but NEVER crash SignalR
                        _logger.LogError(ex, "Failed to persist unread status for user {UserId}", userId);
                    }
                }
            }
        }

        // 4️⃣ Always clean up trackers
        _tracker.LeaveMessagesScreen(connectionId);
        _tracker.LeaveConversation(connectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string senderId, string receiverId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 500)
        {
            _logger.LogWarning("SendMessage rejected: invalid content length. SenderId={SenderId}", senderId);
            return;
        }

        // Per-user rate limiting: max 5 messages per 5 seconds
        var now = DateTime.UtcNow;
        var timestamps = _messageSendTimestamps.GetOrAdd(senderId, _ => new List<DateTime>());
        lock (timestamps)
        {
            timestamps.RemoveAll(t => t < now - MessageRateWindow);
            if (timestamps.Count >= MessageRateLimit)
            {
                _logger.LogWarning("SendMessage rate limit exceeded. SenderId={SenderId}", senderId);
                return;
            }
            timestamps.Add(now);
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        var senderInfo = await GetUserInfoAsync(dbContext, senderId);
        var receiverInfo = await GetUserInfoAsync(dbContext, receiverId);
        if (senderInfo == null || receiverInfo == null) return;

        // Encrypt message
        var encryptedForSender = EncryptionHelper.EncryptString(content, EncryptionHelper.KeyFromBase64(senderInfo.EncryptionKey));
        var encryptedForReceiver = EncryptionHelper.EncryptString(content, EncryptionHelper.KeyFromBase64(receiverInfo.EncryptionKey));

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
            ConversationKey = conversationKey
        };

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(); // message.Id now generated

        // Find or create conversation and save in same round-trip
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

        conversation.LastMessageId = message.Id;
        conversation.LastMessageDate = message.DateTime;
        // Batched: conversation update + potential offline unread flag together below

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
                // User offline → set unread flag and send push notification
                var receiverEntity = await dbContext.Users.FindAsync(receiverId);
                if (receiverEntity != null)
                {
                    receiverEntity.UnseenMessages = true;
                    if (!string.IsNullOrEmpty(receiverEntity.ExpoPushToken))
                        _ = _expoPush.SendBadgeNotificationAsync(receiverEntity.ExpoPushToken);
                }
            }
        }

        await dbContext.SaveChangesAsync(); // single batched save for conversation + unread flag

        // Notify all receiver connections of new message
        if (_userConnections.TryGetValue(receiverId, out var receiverConnections))
        {
            foreach (var connId in receiverConnections)
            {
                await Clients.Client(connId).SendAsync("ReceiveMessage",
                    senderId,
                    content,
                    message.DateTime,
                    senderInfo.ProfileImageUrl,
                    senderInfo.UserName
                );

                await Clients.Client(connId).SendAsync("ReceiveMessageRefetch");
            }
        }
    }


    public async Task BroadcastMessage(string senderId, string[] recipientIds, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 500) return;

        var now = DateTime.UtcNow;
        var timestamps = _messageSendTimestamps.GetOrAdd(senderId, _ => new List<DateTime>());
        lock (timestamps)
        {
            timestamps.RemoveAll(t => t < now - MessageRateWindow);
            if (timestamps.Count >= MessageRateLimit)
            {
                _logger.LogWarning("BroadcastMessage rate limit exceeded. SenderId={SenderId}", senderId);
                return;
            }
            timestamps.Add(now);
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        var senderInfo = await GetUserInfoAsync(dbContext, senderId);
        if (senderInfo == null) return;

        var encryptedForSender = EncryptionHelper.EncryptString(content, EncryptionHelper.KeyFromBase64(senderInfo.EncryptionKey));

        foreach (var receiverId in recipientIds)
        {
            if (receiverId == senderId) continue;

            var receiverInfo = await GetUserInfoAsync(dbContext, receiverId);
            if (receiverInfo == null) continue;

            var encryptedForReceiver = EncryptionHelper.EncryptString(content, EncryptionHelper.KeyFromBase64(receiverInfo.EncryptionKey));

            var conversationKey = string.CompareOrdinal(senderId, receiverId) < 0
                ? senderId + "_" + receiverId
                : receiverId + "_" + senderId;

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                TextSenderEncrypted = encryptedForSender,
                TextReceiverEncrypted = encryptedForReceiver,
                DateTime = now,
                ConversationKey = conversationKey
            };

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync();

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

            conversation.LastMessageId = message.Id;
            conversation.LastMessageDate = message.DateTime;

            bool isReceiverViewingChat = _tracker.IsViewingConversation(receiverId, senderId);
            bool isReceiverOnMessagesScreen = _tracker.IsOnMessagesScreen(receiverId);
            bool isReceiverOnline = _userConnections.ContainsKey(receiverId);

            if (!(isReceiverViewingChat || isReceiverOnMessagesScreen))
            {
                if (isReceiverOnline)
                {
                    _unreadCache[receiverId] = true;
                    if (_userConnections.TryGetValue(receiverId, out var connIds))
                        foreach (var connId in connIds)
                            await Clients.Client(connId).SendAsync("ReceiveUnreadNotification");
                }
                else
                {
                    var receiverEntity = await dbContext.Users.FindAsync(receiverId);
                    if (receiverEntity != null)
                    {
                        receiverEntity.UnseenMessages = true;
                        if (!string.IsNullOrEmpty(receiverEntity.ExpoPushToken))
                            _ = _expoPush.SendBadgeNotificationAsync(receiverEntity.ExpoPushToken);
                    }
                }
            }

            await dbContext.SaveChangesAsync();

            if (_userConnections.TryGetValue(receiverId, out var receiverConnections))
                foreach (var connId in receiverConnections)
                {
                    await Clients.Client(connId).SendAsync("ReceiveMessage", senderId, content, message.DateTime, senderInfo.ProfileImageUrl, senderInfo.UserName);
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

    public Task EnterGroupConversationPage(string userId, string groupId)
    {
        _tracker.EnterConversation(userId, Context.ConnectionId, groupId);
        return Task.CompletedTask;
    }

    public Task LeaveGroupConversationPage(string userId)
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

    private async Task<CachedUserInfo?> GetUserInfoAsync(DataContext dbContext, string userId)
    {
        if (_userInfoCache.TryGetValue(userId, out var cached))
            return cached;

        var user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.EncryptionKey, u.ProfileImageUrl, u.UserName })
            .FirstOrDefaultAsync();

        if (user == null || string.IsNullOrEmpty(user.EncryptionKey))
            return null;

        var info = new CachedUserInfo(user.EncryptionKey, user.ProfileImageUrl ?? string.Empty, user.UserName ?? string.Empty);
        _userInfoCache[userId] = info;
        return info;
    }

    public Task LeaveMessagesScreen(string userId)
    {
        _tracker.LeaveMessagesScreen(Context.ConnectionId); // Remove using connectionId
        return Task.CompletedTask;
    }

    public async Task SendGroupMessage(string senderId, int groupConversationId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 500)
        {
            _logger.LogWarning("SendGroupMessage rejected: invalid content. SenderId={SenderId}", senderId);
            return;
        }

        var now = DateTime.UtcNow;
        var timestamps = _messageSendTimestamps.GetOrAdd(senderId, _ => new List<DateTime>());
        lock (timestamps)
        {
            timestamps.RemoveAll(t => t < now - MessageRateWindow);
            if (timestamps.Count >= MessageRateLimit)
            {
                _logger.LogWarning("SendGroupMessage rate limit exceeded. SenderId={SenderId}", senderId);
                return;
            }
            timestamps.Add(now);
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        var group = await dbContext.GroupConversations.FindAsync(groupConversationId);
        if (group == null) return;

        var isMember = await dbContext.GroupMembers
            .AnyAsync(m => m.GroupConversationId == groupConversationId && m.UserId == senderId);
        if (!isMember) return;

        var senderInfo = await GetUserInfoAsync(dbContext, senderId);
        if (senderInfo == null) return;

        var keyBytes = EncryptionHelper.KeyFromBase64(group.EncryptionKey);
        var encryptedText = EncryptionHelper.EncryptString(content, keyBytes);

        var message = new GroupMessage
        {
            GroupConversationId = groupConversationId,
            SenderId = senderId,
            Text = encryptedText,
            DateTime = now
        };

        dbContext.GroupMessages.Add(message);
        group.LastMessageDate = now;
        await dbContext.SaveChangesAsync();

        var memberIds = await dbContext.GroupMembers
            .Where(m => m.GroupConversationId == groupConversationId)
            .Select(m => m.UserId)
            .ToListAsync();

        foreach (var memberId in memberIds)
        {
            if (!_userConnections.TryGetValue(memberId, out var connections)) continue;

            bool isOnMessagesScreen = _tracker.IsOnMessagesScreen(memberId);
            bool isViewingThisGroup = _tracker.IsViewingConversation(memberId, groupConversationId.ToString());

            bool shouldNotifyUnread = memberId != senderId && !isOnMessagesScreen && !isViewingThisGroup;
            if (shouldNotifyUnread)
            {
                _unreadCache[memberId] = true;
            }

            foreach (var connId in connections)
            {
                if (shouldNotifyUnread)
                {
                    await Clients.Client(connId).SendAsync("ReceiveUnreadNotification");
                }
                if (memberId != senderId)
                {
                    await Clients.Client(connId).SendAsync("ReceiveMessage",
                        senderId,
                        content,
                        now,
                        senderInfo.ProfileImageUrl,
                        senderInfo.UserName
                    );
                }
                await Clients.Client(connId).SendAsync("ReceiveGroupMessageRefetch", groupConversationId);
                await Clients.Client(connId).SendAsync("ReceiveMessageRefetch");
            }
        }

        // Persist unread flag for offline members
        var offlineUnreadMembers = memberIds
            .Where(id => id != senderId && !_userConnections.ContainsKey(id))
            .ToList();

        if (offlineUnreadMembers.Any())
        {
            var offlineUsers = await dbContext.Users
                .Where(u => offlineUnreadMembers.Contains(u.Id))
                .ToListAsync();
            foreach (var u in offlineUsers)
            {
                u.UnseenMessages = true;
                if (!string.IsNullOrEmpty(u.ExpoPushToken))
                    _ = _expoPush.SendBadgeNotificationAsync(u.ExpoPushToken);
            }
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> CheckUnreadMessages(string userId)
    {
        // Try cache first
        if (string.IsNullOrEmpty(userId))
        {
            // Return a default value or throw a controlled exception
            return false; // or throw new ArgumentException("UserId cannot be null");
        }

        if (_unreadCache.TryGetValue(userId, out var cachedValue) && cachedValue is bool hasUnread)
        {
            // Only returns if the cached value exists AND is actually a bool
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