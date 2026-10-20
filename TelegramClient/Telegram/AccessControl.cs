namespace TelegramClient
{
    public interface IAccessControl
    {
        bool IsAllowed(long? userId);
    }
    public class AccessControl : IAccessControl
    {
        private readonly HashSet<long> _allowed;
        public AccessControl(IEnumerable<long> allowedUserIds) => _allowed = allowedUserIds.ToHashSet();
        public bool IsAllowed(long? userId) => userId is { } id && _allowed.Contains(id);
    }
}