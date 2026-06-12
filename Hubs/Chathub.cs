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
using ParrotsAPI2.Services.Notifications;


public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationPageTracker _tracker;
    private readonly ExpoPushService _expoPush;
    // userId → set of active connections with foreground state (one user can have multiple tabs/devices)
    private static readonly ConcurrentDictionary<string, HashSet<ConnectionInfo>> _userConnections = new();
    // userId → badge count (resets when user foregrounds the app)
    private static readonly ConcurrentDictionary<string, int> _userBadgeCounts = new();
    // userId → total unread message count across all conversations (persisted to DB on disconnect)
    private static readonly ConcurrentDictionary<string, int> _unreadCache = new();
    private static readonly ConcurrentDictionary<string, List<DateTime>> _messageSendTimestamps = new();
    // userId → EncryptionKey, ProfileImageUrl, UserName (populated on first message, avoids repeated DB lookups)
    private static readonly ConcurrentDictionary<string, CachedUserInfo> _userInfoCache = new();
    private const int MessageRateLimit = 5;
    private static readonly TimeSpan MessageRateWindow = TimeSpan.FromSeconds(5);

    private record CachedUserInfo(string EncryptionKey, string ProfileImageUrl, string ProfileImageThumbnailUrl, string UserName, string PublicId);
    internal record ConnectionInfo(string ConnectionId, bool IsForeground, bool IsWeb);

    public static IReadOnlyDictionary<string, IEnumerable<string>> GetUserConnectionIds() =>
        _userConnections.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(c => c.ConnectionId));
    public static IReadOnlyDictionary<string, int> GetUnreadCache() => _unreadCache;
    public static void SeedBadgeCounts(Dictionary<string, int> counts)
    {
        foreach (var kvp in counts)
            _userBadgeCounts[kvp.Key] = kvp.Value;
    }
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
        var isWeb = Context.GetHttpContext()?.Request.Query["platform"].ToString() == "web";
        if (!string.IsNullOrEmpty(userId))
        {

            _userConnections.AddOrUpdate(
                    userId,
                    _ => new HashSet<ConnectionInfo> { new(Context.ConnectionId, true, isWeb) },
                    (_, existingSet) =>
                    {
                        lock (existingSet)
                        {
                            existingSet.Add(new(Context.ConnectionId, true, isWeb));
                        }
                        return existingSet;
                    });

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var hasUnread = await dbContext.UnreadConversations
                .AnyAsync(u => u.UserId == userId && u.Count > 0);
            if (hasUnread)
                await Clients.Caller.SendAsync("ReceiveUnreadNotification");
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
            if (kvp.Value.Any(c => c.ConnectionId == connectionId))
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
                connections.RemoveWhere(c => c.ConnectionId == connectionId);
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

            }
        }

        // 4️⃣ Always clean up trackers
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
        bool isReceiverOnline = _userConnections.TryGetValue(receiverId, out var receiverConns);
        bool isReceiverForeground = isReceiverOnline && receiverConns!.Any(c => c.IsForeground && !c.IsWeb);

        if (!isReceiverForeground)
        {
            var badgeCount = await UpsertUnreadAndGetTotalAsync(dbContext, receiverId, conversationKey);
            var receiverEntity = await dbContext.Users.FindAsync(receiverId);
            if (receiverEntity != null && !string.IsNullOrEmpty(receiverEntity.ExpoPushToken))
            {
                var pushBadge = _userBadgeCounts.AddOrUpdate(receiverId, 1, (_, old) => old + 1);
                _logger.LogInformation("[PUSH] Receiver backgrounded/offline → sending push. Token: {Token}", receiverEntity.ExpoPushToken);
                _ = _expoPush.SendBadgeNotificationAsync(receiverEntity.ExpoPushToken, senderInfo.UserName, pushBadge);
            }
        }
        else if (!isReceiverViewingChat)
        {
            var badgeCount = await UpsertUnreadAndGetTotalAsync(dbContext, receiverId, conversationKey);

            if (isReceiverOnline)
                foreach (var connId in receiverConns!.Select(c => c.ConnectionId))
                    await Clients.Client(connId).SendAsync("ReceiveUnreadNotification");

            _logger.LogInformation("[PUSH] Receiver {ReceiverId} in foreground → push skipped", receiverId);
        }

        await dbContext.SaveChangesAsync(); // single batched save for conversation + unread flag

        // Query last 5 messages for this conversation
        var rawLast5 = await dbContext.Messages
            .Where(m => m.ConversationKey == conversationKey)
            .OrderByDescending(m => m.DateTime)
            .Take(3)
            .OrderBy(m => m.DateTime)
            .ToListAsync();

        // Decrypt for receiver perspective
        var last5ForReceiver = rawLast5.Select(m => new {
            id = m.Id,
            senderId = m.SenderId,
            text = m.SenderId == senderId
                ? EncryptionHelper.DecryptString(m.TextReceiverEncrypted, EncryptionHelper.KeyFromBase64(receiverInfo.EncryptionKey))
                : EncryptionHelper.DecryptString(m.TextSenderEncrypted, EncryptionHelper.KeyFromBase64(receiverInfo.EncryptionKey)),
            dateTime = m.DateTime,
            senderProfileImageUrl = m.SenderId == senderId ? senderInfo.ProfileImageUrl : receiverInfo.ProfileImageUrl,
            senderProfileThumbnailUrl = m.SenderId == senderId ? senderInfo.ProfileImageThumbnailUrl : receiverInfo.ProfileImageThumbnailUrl,
            senderUsername = m.SenderId == senderId ? senderInfo.UserName : receiverInfo.UserName,
            senderPublicId = m.SenderId == senderId ? senderInfo.PublicId : receiverInfo.PublicId,
        }).ToList();

        // Decrypt for sender perspective
        var last5ForSender = rawLast5.Select(m => new {
            id = m.Id,
            senderId = m.SenderId,
            text = m.SenderId == senderId
                ? EncryptionHelper.DecryptString(m.TextSenderEncrypted, EncryptionHelper.KeyFromBase64(senderInfo.EncryptionKey))
                : EncryptionHelper.DecryptString(m.TextReceiverEncrypted, EncryptionHelper.KeyFromBase64(receiverInfo.EncryptionKey)),
            dateTime = m.DateTime,
            senderProfileImageUrl = m.SenderId == senderId ? senderInfo.ProfileImageUrl : receiverInfo.ProfileImageUrl,
            senderUsername = m.SenderId == senderId ? senderInfo.UserName : receiverInfo.UserName,
            senderPublicId = m.SenderId == senderId ? senderInfo.PublicId : receiverInfo.PublicId,
        }).ToList();

        if (_userConnections.TryGetValue(receiverId, out var receiverConnections))
            foreach (var connId in receiverConnections.Select(c => c.ConnectionId))
                await Clients.Client(connId).SendAsync("ReceiveMessage", last5ForReceiver);
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
            bool isReceiverOnline = _userConnections.TryGetValue(receiverId, out var bcastReceiverConns);
            bool isReceiverForeground = isReceiverOnline && bcastReceiverConns!.Any(c => c.IsForeground);

            if (!isReceiverForeground)
            {
                var badgeCount = await UpsertUnreadAndGetTotalAsync(dbContext, receiverId, conversationKey);
                var receiverEntity = await dbContext.Users.FindAsync(receiverId);
                if (receiverEntity != null && !string.IsNullOrEmpty(receiverEntity.ExpoPushToken))
                {
                    var pushBadge = _userBadgeCounts.AddOrUpdate(receiverId, 1, (_, old) => old + 1);
                    _ = _expoPush.SendBadgeNotificationAsync(receiverEntity.ExpoPushToken, senderInfo.UserName, pushBadge);
                }
            }
            else if (!isReceiverViewingChat)
            {
                var badgeCount = await UpsertUnreadAndGetTotalAsync(dbContext, receiverId, conversationKey);

                if (isReceiverOnline)
                    foreach (var connId in bcastReceiverConns!.Select(c => c.ConnectionId))
                        await Clients.Client(connId).SendAsync("ReceiveUnreadNotification");
            }

            await dbContext.SaveChangesAsync();

            if (_userConnections.TryGetValue(receiverId, out var receiverConnections))
                foreach (var connId in receiverConnections.Select(c => c.ConnectionId))
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

    public Task UpdatePresence(bool isForeground)
    {
        var connectionId = Context.ConnectionId;
        foreach (var kvp in _userConnections)
        {
            lock (kvp.Value)
            {
                var existing = kvp.Value.FirstOrDefault(c => c.ConnectionId == connectionId);
                if (existing != null)
                {
                    kvp.Value.Remove(existing);
                    kvp.Value.Add(new ConnectionInfo(connectionId, isForeground, existing.IsWeb));
                    if (isForeground && !existing.IsWeb)
                        _userBadgeCounts[kvp.Key] = 0;
                    break;
                }
            }
        }
        return Task.CompletedTask;
    }

    private async Task<int> UpsertUnreadAndGetTotalAsync(DataContext dbContext, string userId, string conversationKey)
    {
        var existing = await dbContext.UnreadConversations
            .FirstOrDefaultAsync(u => u.UserId == userId && u.ConversationKey == conversationKey);

        if (existing == null)
            dbContext.UnreadConversations.Add(new UnreadConversation { UserId = userId, ConversationKey = conversationKey, Count = 1, LastUpdated = DateTime.UtcNow });
        else
        {
            existing.Count += 1;
            existing.LastUpdated = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        return await dbContext.UnreadConversations
            .Where(u => u.UserId == userId)
            .SumAsync(u => u.Count);
    }

    private async Task ResetUnreadAsync(DataContext dbContext, string userId, string conversationKey)
    {
        var existing = await dbContext.UnreadConversations
            .FirstOrDefaultAsync(u => u.UserId == userId && u.ConversationKey == conversationKey);

        if (existing != null && existing.Count > 0)
        {
            existing.Count = 0;
            existing.LastUpdated = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task<CachedUserInfo?> GetUserInfoAsync(DataContext dbContext, string userId)
    {
        if (_userInfoCache.TryGetValue(userId, out var cached))
            return cached;

        var user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.EncryptionKey, u.ProfileImageUrl, u.ProfileImageThumbnailUrl, u.UserName, u.PublicId })
            .FirstOrDefaultAsync();

        if (user == null || string.IsNullOrEmpty(user.EncryptionKey))
            return null;

        var info = new CachedUserInfo(user.EncryptionKey, user.ProfileImageUrl ?? string.Empty, user.ProfileImageThumbnailUrl ?? string.Empty, user.UserName ?? string.Empty, user.PublicId ?? string.Empty);
        _userInfoCache[userId] = info;
        return info;
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

            bool isViewingThisGroup = _tracker.IsViewingConversation(memberId, groupConversationId.ToString());
            bool isMemberForeground = connections.Any(c => c.IsForeground && !c.IsWeb);
            bool shouldNotifyUnread = memberId != senderId && !isViewingThisGroup;
            if (shouldNotifyUnread && isMemberForeground)
            {
                var groupConvKey = $"group_{groupConversationId}";
                await UpsertUnreadAndGetTotalAsync(dbContext, memberId, groupConvKey);
            }

            foreach (var connId in connections.Select(c => c.ConnectionId))
            {
                if (shouldNotifyUnread)
                    await Clients.Client(connId).SendAsync("ReceiveUnreadNotification");
            }
        }

        // Query last 5 group messages and send to all online members
        var last5GroupRaw = await (
            from m in dbContext.GroupMessages
            join u in dbContext.Users on m.SenderId equals u.Id
            where m.GroupConversationId == groupConversationId
            orderby m.DateTime descending
            select new {
                id = m.Id,
                senderId = m.SenderId,
                encryptedText = m.Text,
                dateTime = m.DateTime,
                senderProfileImageUrl = u.ProfileImageUrl ?? "",
                senderProfileThumbnailUrl = u.ProfileImageThumbnailUrl ?? "",
                senderUsername = u.UserName ?? "",
                senderPublicId = u.PublicId ?? "",
            })
            .Take(3)
            .OrderBy(m => m.dateTime)
            .ToListAsync();

        var last5Group = last5GroupRaw.Select(m => new {
            m.id,
            m.senderId,
            text = EncryptionHelper.DecryptString(m.encryptedText, keyBytes),
            m.dateTime,
            m.senderProfileImageUrl,
            m.senderProfileThumbnailUrl,
            m.senderUsername,
            m.senderPublicId,
        }).ToList();

        foreach (var memberId in memberIds)
        {
            if (!_userConnections.TryGetValue(memberId, out var memberConns)) continue;
            foreach (var connId in memberConns.Select(c => c.ConnectionId))
                await Clients.Client(connId).SendAsync("ReceiveGroupMessage", last5Group);
        }

        // Persist unread flag for offline or backgrounded members
        var pushTargetMembers = memberIds
            .Where(id => id != senderId && (!_userConnections.ContainsKey(id) || !_userConnections[id].Any(c => c.IsForeground && !c.IsWeb)))
            .ToList();

        if (pushTargetMembers.Any())
        {
            var groupConvKey = $"group_{groupConversationId}";
            var targetUsers = await dbContext.Users
                .Where(u => pushTargetMembers.Contains(u.Id))
                .ToListAsync();
            foreach (var u in targetUsers)
            {
                var badgeCount = await UpsertUnreadAndGetTotalAsync(dbContext, u.Id, groupConvKey);
                if (!string.IsNullOrEmpty(u.ExpoPushToken))
                {
                    var pushBadge = _userBadgeCounts.AddOrUpdate(u.Id, 1, (_, old) => old + 1);
                    _ = _expoPush.SendBadgeNotificationAsync(u.ExpoPushToken, senderInfo.UserName, pushBadge);
                }
            }
        }
    }

    public async Task<int> CheckUnreadMessages(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return 0;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await dbContext.UnreadConversations
            .Where(u => u.UserId == userId)
            .SumAsync(u => u.Count);
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