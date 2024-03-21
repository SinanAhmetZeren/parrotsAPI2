using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParrotsAPI2.Models;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

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

        /*
        public override async Task OnConnectedAsync2()
        {
            _logger.LogInformation($"User connected: ConnectionId={Context.ConnectionId}");

            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                user.ConnectionId = Context.ConnectionId;
                await _userManager.UpdateAsync(user);
            }
            await base.OnConnectedAsync();
        }
        */
         
        public override async Task OnConnectedAsync()
        {
            // var userId = Context.User.Identity.Name; // Assuming you're using JWT authentication

            var userId = Context.GetHttpContext().Request.Query["userId"].ToString();

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

        public override async Task OnDisconnectedAsync(Exception exception)
        {

            var userId = Context.GetHttpContext().Request.Query["userId"].ToString();
            _logger.LogInformation($"User disconnected: ConnectionId={Context.ConnectionId}, UserId={userId}");

            //var user = await _userManager.GetUserAsync(Context.User);
            //if (user != null)
            //{
            //    user.ConnectionId = null;
            //    await _userManager.UpdateAsync(user);
            //}

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
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = content,
                DateTime = DateTime.UtcNow,
                Rendered = false,
                ReadByReceiver = false
            };
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();
            var receiverConnectionId = await GetConnectionIdForUser(receiverId);
            if (receiverConnectionId != null)
            {
                // await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", senderId, content);
                // await Clients.All.SendAsync("ReceiveMessage", senderId, content);
                //await Clients.All.SendAsync("ReceiveMessage", content);

                // await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
            }
            return;

        }

        public async Task GetMessages(string userId)
        {
            var messages = await _dbContext.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderBy(m => m.DateTime)
                .ToListAsync();
            await Clients.Caller.SendAsync("ReceiveMessages", messages);
        }

        public async Task WriteMessageToDb(string senderId, string receiverId, string content)
        {
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = content,
                DateTime = DateTime.UtcNow,
                Rendered = false,
                ReadByReceiver = false
            };

            Console.WriteLine("-------------------------------------------------");

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