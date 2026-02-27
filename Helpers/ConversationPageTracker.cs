using System.Collections.Concurrent;

public class ConversationPageTracker
{
    // user is on Messages screen
    private readonly ConcurrentDictionary<string, bool> _messagesScreenOpen = new();

    // user is viewing specific conversationuserId -> partnerId
    private readonly ConcurrentDictionary<string, string> _activeConversation = new();
    public void EnterMessagesScreen(string userId) { _messagesScreenOpen[userId] = true; }
    public void LeaveMessagesScreen(string userId) { _messagesScreenOpen.TryRemove(userId, out _); }
    public bool IsOnMessagesScreen(string userId) { return _messagesScreenOpen.ContainsKey(userId); }
    public void EnterConversation(string userId, string partnerId) { _activeConversation[userId] = partnerId; }
    public void LeaveConversation(string userId) { _activeConversation.TryRemove(userId, out _); }
    public bool IsViewingConversation(string userId, string partnerId) { return _activeConversation.TryGetValue(userId, out var activePartner) && activePartner == partnerId; }




    public IEnumerable<string> GetAllUsersOnMessagesScreen()
    {
        return _messagesScreenOpen.Keys;
    }

}