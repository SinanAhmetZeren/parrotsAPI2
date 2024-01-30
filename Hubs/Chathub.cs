using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParrotsAPI2.Models;
using System;

namespace ParrotsAPI2.Hubs
{
    public class ChatHub : Hub<IChatHub>
    {
        private readonly DataContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
            _logger.LogInformation("ChatHub initialized");
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected!");
            Console.WriteLine($"Client connected with connection id: {Context.ConnectionId}");
            Clients.Client(Context.ConnectionId).ReceiveMessage("111","Hello! Welcome to the chat.");
            return base.OnConnectedAsync();
        }


        public async Task SendMessage(string message)
        {
            _logger.LogInformation($"Received message: {message}");
            _ = Clients.Client(Context.ConnectionId).ReceiveMessage("222", "Hello there!");
        }

        public async Task SendMessage2(string user, string message)
        {
            try
            {
                _logger.LogInformation($"Received message from {user}: {message}");
                var chatMessage = new Message
                {
                    Text = message,
                    DateTime = DateTime.UtcNow,
                    Rendered = false,
                    ReadByReceiver = false,
                    SenderId = "",
                    ReceiverId = ""
                };

                Console.WriteLine(chatMessage);
                //_context.Messages.Add(chatMessage);
                //await _context.SaveChangesAsync();
                _ = Clients.Client(Context.ConnectionId).ReceiveMessage("Server", "Hello! Welcome to the chat.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendMessage: {ex.Message}");
                throw;
            }
        }

    }
}

/*
start connection:
{ "protocol":"json","version":1}

single argument:
{ "arguments":["xyz!"],"invocationId":"0","target":"SendMessage","type":1}

two arguments:
{ "arguments":["arg1!","arg2!!"],"invocationId":"0","target":"SendMessage2","type":1}
*/