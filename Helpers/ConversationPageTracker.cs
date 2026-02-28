using System.Collections.Concurrent;

public class ConversationPageTracker
{

    private readonly ConcurrentDictionary<string, string> _messagesScreenOpen = new(); // connectionId -> userId
    private readonly ConcurrentDictionary<string, (string UserId, string PartnerId)> _activeConversation = new();// connectionId -> (userId, partnerId)

    public void EnterMessagesScreen(string userId, string connectionId) => _messagesScreenOpen[connectionId] = userId;
    public void LeaveMessagesScreen(string connectionId) => _messagesScreenOpen.TryRemove(connectionId, out _);
    public bool IsOnMessagesScreen(string userId) => _messagesScreenOpen.Values.Any(uid => uid == userId);
    public void EnterConversation(string userId, string connectionId, string partnerId) => _activeConversation[connectionId] = (userId, partnerId);
    public void LeaveConversation(string connectionId) => _activeConversation.TryRemove(connectionId, out _);
    public bool IsViewingConversation(string userId, string partnerId) => _activeConversation.Values.Any(x => x.UserId == userId && x.PartnerId == partnerId);

    public void RemoveConnection(string connectionId)
    {
        _messagesScreenOpen.TryRemove(connectionId, out _);
        _activeConversation.TryRemove(connectionId, out _);
    }
}