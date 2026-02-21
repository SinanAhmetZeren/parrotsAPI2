using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ParrotsAPI2.Models;
using ParrotsAPI2.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using ParrotsAPI2.Migrations;

namespace ParrotsAPI2.Hubs
{
    public class ChatHub3 : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConversationPageTracker _tracker;

        public ChatHub3(
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
            await base.OnConnectedAsync();
            await Clients.Caller.SendAsync("ParrotsChatHubInitialized");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString() ?? "";
            if (!string.IsNullOrEmpty(userId))
            {
                _tracker.LeaveMessagesScreen(userId);
                _tracker.LeaveConversation(userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task MarkMessagesAsSeen(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var user = await dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.UnseenMessages = false;
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task SendMessage(string senderId, string receiverId, string content)
        {
            var newTime = DateTime.UtcNow;

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            var sender = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == senderId);
            var receiver = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == receiverId);

            if (sender == null || string.IsNullOrEmpty(sender.EncryptionKey))
                throw new Exception("Sender or encryption key not found.");
            if (receiver == null || string.IsNullOrEmpty(receiver.EncryptionKey))
                throw new Exception("Receiver or encryption key not found.");

            var senderKeyBytes = EncryptionHelper.KeyFromBase64(sender.EncryptionKey);
            var receiverKeyBytes = EncryptionHelper.KeyFromBase64(receiver.EncryptionKey);

            // Encrypt content
            var encryptedForSender = EncryptionHelper.EncryptString(content, senderKeyBytes);
            var encryptedForReceiver = EncryptionHelper.EncryptString(content, receiverKeyBytes);

            // Save message
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

            var receiverConnectionId = await GetConnectionIdForUser(receiverId);


            // if user is NOT on messages screen AND 
            // NOT viewing this conversation, mark as unseen
            if (!_tracker.IsOnMessagesScreen(receiverId) &&
            !_tracker.IsViewingConversation(receiverId, senderId))
            {
                receiver.UnseenMessages = true;
                await Clients.User(receiverId).SendAsync("UnreadMessagesStatusTrue");
                Console.WriteLine($"xx --> User {receiverId} --> UnreadMessagesStatusTrue");

            }
            await dbContext.SaveChangesAsync();
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, content, newTime,
                sender.ProfileImageUrl ?? string.Empty, sender.UserName ?? string.Empty);
            await Clients.All.SendAsync("ReceiveMessage", senderId, content, newTime,
                sender.ProfileImageUrl ?? string.Empty, sender.UserName ?? string.Empty);

            await Clients.User(receiverId).SendAsync("ReceiveMessageRefetch");
        }

        public Task EnterConversationPage(string userId, string partnerId)
        {
            _tracker.EnterConversation(userId, partnerId);
            _logger.LogInformation($"->>> User entered conversation");
            return Task.CompletedTask;
        }

        public Task LeaveConversationPage(string userId)
        {
            _tracker.LeaveConversation(userId);
            _logger.LogInformation($"User left conversation");
            return Task.CompletedTask;
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
            _logger.LogInformation($"User entered messages screen");
        }

        public Task LeaveMessagesScreen(string userId)
        {
            _tracker.LeaveMessagesScreen(userId);
            _logger.LogInformation($"User left messages screen");
            return Task.CompletedTask;
        }

        public async Task<bool> CheckUnreadMessages(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var user = await dbContext.Users.FindAsync(userId);
            return user?.UnseenMessages ?? false;
        }

        private async Task<string?> GetConnectionIdForUser(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var user = await dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                var connectionId = user.ConnectionId;
                return connectionId;
            }
            return null;
        }

    }
}