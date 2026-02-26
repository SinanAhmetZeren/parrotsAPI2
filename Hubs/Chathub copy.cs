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

namespace ParrotsAPI2.Hubs
{
    public class ChatHub2 : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConversationPageTracker _tracker;

        // The "Magic" Dictionary: Maps UserId -> ConnectionId in RAM
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();

        public ChatHub2(
            ILogger<ChatHub> logger,
            IServiceScopeFactory scopeFactory,
            ConversationPageTracker tracker
            )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString() ?? string.Empty;
            using (var scope = _scopeFactory.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.ConnectionId = Context.ConnectionId;
                    try
                    {
                        await userManager.UpdateAsync(user);
                        await dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error updating user connectionId: {ex.Message}");
                    }
                }
            }
            await base.OnConnectedAsync();
            await Clients.Caller.SendAsync("ParrotsChatHubInitialized");
        }



        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(userId))
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null && user.ConnectionId == Context.ConnectionId)
                    {
                        try
                        {
                            user.ConnectionId = null;
                            await userManager.UpdateAsync(user);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error updating user connectionId on disconnect. UserId={UserId}",
                                userId
                            );
                        }
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessage(string senderId, string receiverId, string content)
        {
            var newTime = DateTime.UtcNow;
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var sender = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == senderId);
                var receiver = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == receiverId);
                if (sender == null || string.IsNullOrEmpty(sender.EncryptionKey))
                    throw new HubException("Sender or encryption key not found.");
                if (receiver == null || string.IsNullOrEmpty(receiver.EncryptionKey))
                    throw new HubException("Receiver or encryption key not found.");
                var senderKeyBytes = EncryptionHelper.KeyFromBase64(sender.EncryptionKey);
                var receiverKeyBytes = EncryptionHelper.KeyFromBase64(receiver.EncryptionKey);
                // 2️⃣ Encrypt content for both sender and receiver
                var encryptedForSender = EncryptionHelper.EncryptString(content, senderKeyBytes);
                var encryptedForReceiver = EncryptionHelper.EncryptString(content, receiverKeyBytes);
                // 3️⃣ Create and save the message
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    TextSenderEncrypted = encryptedForSender,
                    TextReceiverEncrypted = encryptedForReceiver,
                    DateTime = newTime,
                    Rendered = false,
                    ReadByReceiver = false
                };

                dbContext.Messages.Add(message);

                // --- Logic for UnseenMessages Flag ---
                bool isReceiverViewingChat = _tracker.IsViewingConversation(receiverId, senderId);
                bool isReceiverOnMessagesScreen = _tracker.IsOnMessagesScreen(receiverId);

                // If NOT in the specific chat AND NOT on the messages list, flag as true
                if (!isReceiverViewingChat && !isReceiverOnMessagesScreen)
                {
                    receiver.UnseenMessages = true;
                    // Notify the client in real-time about the new unread status
                    if (!string.IsNullOrEmpty(receiver.ConnectionId))
                    {
                        await Clients.Client(receiver.ConnectionId).SendAsync("ReceiveUnreadNotification");
                        var x = 0;
                    }
                }

                await dbContext.SaveChangesAsync();

                // 4️⃣ Prepare sender metadata for the SignalR call
                var senderProfileUrl = sender.ProfileImageUrl ?? string.Empty;
                var senderUsername = sender.UserName ?? string.Empty;
                var receiverConnectionId = receiver.ConnectionId; // Optimization: Use the ID we just fetched
                // 5️⃣ Send the message via SignalR
                if (!string.IsNullOrEmpty(receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId)
                        .SendAsync("ReceiveMessage", senderId, content, newTime, senderProfileUrl, senderUsername);

                    await Clients.Client(receiverConnectionId)
                        .SendAsync("ReceiveMessageRefetch");
                }
            }
        }
        public async Task<bool> CheckUnreadMessages(string userId)
        {
            // this is for app launch only
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var user = await dbContext.Users.FindAsync(userId);
                return user?.UnseenMessages ?? false;
            }
        }
        public async Task MarkMessagesAsSeen(string userId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {

                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var user = await dbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    user.UnseenMessages = false;
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        public async Task EnterConversationPage(string userId, string partnerId)
        {
            _tracker.EnterConversation(userId, partnerId);
            _logger.LogInformation($"->>> User entered conversation");
            await MarkMessagesAsSeen(userId);
        }
        public async Task LeaveConversationPage(string userId)
        {
            _tracker.LeaveConversation(userId);
            _logger.LogInformation($"User left conversation");

        }
        public async Task EnterMessagesScreen(string userId)
        {
            _tracker.EnterMessagesScreen(userId);
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var user = await dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.UnseenMessages = false;
                await dbContext.SaveChangesAsync();
            }

            var allUsers = _tracker.GetAllUsersOnMessagesScreen();
            _logger.LogInformation("All users on Messages Screen: {Users}", string.Join(", ", allUsers));
            _logger.LogInformation($"User entered messages screen");
        }
        public async Task LeaveMessagesScreen(string userId)
        {
            _tracker.LeaveMessagesScreen(userId);
            _logger.LogInformation($"User left messages screen");

            var allUsers = _tracker.GetAllUsersOnMessagesScreen();
            _logger.LogInformation("All users on Messages Screen: {Users}", string.Join(", ", allUsers));


        }


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