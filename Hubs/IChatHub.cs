namespace ParrotsAPI2.Hubs
{
    public interface IChatHub
    {
        Task ReceiveMessage(string user, string message);
    }
}