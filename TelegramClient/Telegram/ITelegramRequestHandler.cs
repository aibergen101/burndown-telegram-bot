namespace TelegramClient
{
    public interface ITelegramRequestHandler
    {
        Task HandleRequest(string request);
    }
}
