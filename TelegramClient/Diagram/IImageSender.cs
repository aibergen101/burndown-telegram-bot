namespace TelegramClient 
{ 
    public interface IImageSender
    {
        Task SendDiagram(string chatId, byte[] photoData);
    }
}