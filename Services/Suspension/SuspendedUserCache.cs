namespace ParrotsAPI2.Services.Suspension
{
    public class SuspendedUserCache
    {
        private readonly HashSet<string> _suspendedIds = new();
        private readonly object _lock = new();

        public void Load(IEnumerable<string> userIds)
        {
            lock (_lock)
            {
                _suspendedIds.Clear();
                foreach (var id in userIds)
                    _suspendedIds.Add(id);
            }
        }

        public void Add(string userId)
        {
            lock (_lock) _suspendedIds.Add(userId);
        }

        public void Remove(string userId)
        {
            lock (_lock) _suspendedIds.Remove(userId);
        }

        public bool IsSuspended(string userId)
        {
            lock (_lock) return _suspendedIds.Contains(userId);
        }
    }
}
