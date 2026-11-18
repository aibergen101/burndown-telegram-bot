namespace TelegramClient
{
    public interface IAccessControl
    {
        bool IsAllowed(long? userId);
    }
    // Контракт и реализацию лучше разнести по разным файлам и папкам в целом
    public class AccessControl : IAccessControl
    {
        private readonly HashSet<long> _allowed;
        public AccessControl(IEnumerable<long> allowedUserIds) => _allowed = allowedUserIds.ToHashSet();
        public bool IsAllowed(long? userId) => userId is { } id && _allowed.Contains(id);
    }
}