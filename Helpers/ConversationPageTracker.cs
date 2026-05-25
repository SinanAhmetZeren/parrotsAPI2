using System.Collections.Concurrent;

public class ConversationPageTracker
{

    private readonly ConcurrentDictionary<string, (string UserId, string PartnerId)> _activeConversation = new();// connectionId -> (userId, partnerId)

    public void EnterConversation(string userId, string connectionId, string partnerId) => _activeConversation[connectionId] = (userId, partnerId);
    public void LeaveConversation(string connectionId) => _activeConversation.TryRemove(connectionId, out _);
    public bool IsViewingConversation(string userId, string partnerId) => _activeConversation.Values.Any(x => x.UserId == userId && x.PartnerId == partnerId);

}