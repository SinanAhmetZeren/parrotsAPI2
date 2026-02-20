using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ParrotsAPI2.Models;
using ParrotsAPI2.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ParrotsAPI2.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConversationPageTracker _tracker;

        public ChatHub(
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
            if (!_tracker.IsOnMessagesScreen(receiverId) &&
            !_tracker.IsViewingConversation(receiverId, senderId))
            {
                receiver.UnseenMessages = true;
            }
            await dbContext.SaveChangesAsync();
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, content, newTime,
                sender.ProfileImageUrl ?? string.Empty, sender.UserName ?? string.Empty);

            await Clients.User(receiverId).SendAsync("ReceiveMessageRefetch");
        }

        public Task EnterConversationPage(string userId, string partnerId)
        {
            _tracker.EnterConversation(userId, partnerId);
            _logger.LogInformation($"User {userId} entered conversation with {partnerId}");
            return Task.CompletedTask;
        }

        public Task LeaveConversationPage(string userId)
        {
            _tracker.LeaveConversation(userId);
            _logger.LogInformation($"User {userId} left conversation");
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
            _logger.LogInformation($"User {userId} entered messages screen → marked all as read");
        }

        public Task LeaveMessagesScreen(string userId)
        {
            _tracker.LeaveMessagesScreen(userId);
            _logger.LogInformation($"User {userId} left messages screen");
            return Task.CompletedTask;
        }



    }
}