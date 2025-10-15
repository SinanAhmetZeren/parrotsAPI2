﻿using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParrotsAPI2.Models;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ParrotsAPI2.Helpers;

namespace ParrotsAPI2.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly DataContext _dbContext;
        private readonly UserManager<AppUser> _userManager;

        public ChatHub(
            ILogger<ChatHub> logger,
            DataContext dbContext,
            UserManager<AppUser> userManager
            )
        {
            _logger = logger;
            _dbContext = dbContext;
            _userManager = userManager;
            _logger.LogInformation($"--> ChatHub initialized at {DateTime.UtcNow}");
        }


        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString() ?? string.Empty;
            _logger.LogInformation($"User connected: ConnectionId={Context.ConnectionId}, UserId={userId}");
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.ConnectionId = Context.ConnectionId;
                try
                {
                    await _userManager.UpdateAsync(user);
                    await _dbContext.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating user connectionId: {ex.Message}");
                }
            }
            await base.OnConnectedAsync();
        }



        public override async Task OnDisconnectedAsync(Exception? exception)
        {

            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString() ?? string.Empty;
            _logger.LogInformation($"User disconnected: ConnectionId={Context.ConnectionId}, UserId={userId}");
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.ConnectionId = null;
                try
                {
                    await _userManager.UpdateAsync(user);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating user connectionId on disconnect: {ex.Message}");
                }
            }
            await base.OnDisconnectedAsync(exception);
        }


        public async Task SendMessage(string senderId, string receiverId, string content)
        {
            var newTime = DateTime.UtcNow;

            // 1️⃣ Get sender and receiver encryption keys
            var sender = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == senderId);
            var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == receiverId);

            if (sender == null || string.IsNullOrEmpty(sender.EncryptionKey))
                throw new Exception("Sender or encryption key not found.");
            if (receiver == null || string.IsNullOrEmpty(receiver.EncryptionKey))
                throw new Exception("Receiver or encryption key not found.");

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

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            // 4️⃣ Prepare sender metadata
            var senderProfileUrl = sender?.ProfileImageUrl ?? string.Empty;
            var senderUsername = sender?.UserName ?? string.Empty;

            // 5️⃣ Send the message via SignalR (original content)
            var receiverConnectionId = await GetConnectionIdForUser(receiverId);
            if (receiverConnectionId != null)
            {
                await Clients.Client(receiverConnectionId)
                    .SendAsync("ReceiveMessage", senderId, content, newTime, senderProfileUrl, senderUsername);

                await Clients.Client(receiverConnectionId)
                    .SendAsync("ReceiveMessageRefetch");
            }
        }


        public async Task GetMessages(string userId)
        {
            // 1️⃣ Get the current user's encryption key
            var currentUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null || string.IsNullOrEmpty(currentUser.EncryptionKey))
                throw new Exception("User or encryption key not found.");

            var currentUserKeyBytes = EncryptionHelper.KeyFromBase64(currentUser.EncryptionKey);

            // 2️⃣ Fetch messages where user is sender or receiver
            var messages = await _dbContext.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            // 3️⃣ Decrypt each message for this user
            var decryptedMessages = messages.Select(m => new
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Text = m.SenderId == userId
                    ? EncryptionHelper.DecryptString(m.TextSenderEncrypted, currentUserKeyBytes)
                    : EncryptionHelper.DecryptString(m.TextReceiverEncrypted, currentUserKeyBytes),
                DateTime = m.DateTime,
                Rendered = m.Rendered,
                ReadByReceiver = m.ReadByReceiver
            }).ToList();

            // 4️⃣ Send decrypted messages to client
            await Clients.Caller.SendAsync("ReceiveMessages", decryptedMessages);
        }



        public async Task WriteMessageToDb(string senderId, string receiverId, string content)
        {
            // 1️⃣ Get sender and receiver
            var sender = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == senderId);
            var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == receiverId);

            if (sender == null || string.IsNullOrEmpty(sender.EncryptionKey))
                throw new Exception("Sender or encryption key not found.");
            if (receiver == null || string.IsNullOrEmpty(receiver.EncryptionKey))
                throw new Exception("Receiver or encryption key not found.");

            var senderKeyBytes = EncryptionHelper.KeyFromBase64(sender.EncryptionKey);
            var receiverKeyBytes = EncryptionHelper.KeyFromBase64(receiver.EncryptionKey);

            // 2️⃣ Encrypt content for both sender and receiver
            var encryptedForSender = EncryptionHelper.EncryptString(content, senderKeyBytes);
            var encryptedForReceiver = EncryptionHelper.EncryptString(content, receiverKeyBytes);

            // 3️⃣ Create and save message
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                TextSenderEncrypted = encryptedForSender,
                TextReceiverEncrypted = encryptedForReceiver,
                DateTime = DateTime.UtcNow,
                Rendered = false,
                ReadByReceiver = false
            };

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();
        }


        private async Task<string?> GetConnectionIdForUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var connectionId = user.ConnectionId;
                return connectionId;
            }
            return null;
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