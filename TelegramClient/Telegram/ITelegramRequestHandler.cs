namespace TelegramClient
{
    public interface ITelegramRequestHandler
    {
        // Из интерфейса непонятно, что за request тут, в каком он формате и тд. На деле это кажется вообще ИД чата
        // Можно кроме переименования пойти дальше, и передавать сюда ValueObject (читать про ValueObject DDD + Primitive obsession)
        Task HandleRequest(string request, ChartTheme theme);
    }
}
