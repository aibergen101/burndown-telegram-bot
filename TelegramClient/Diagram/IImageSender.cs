namespace TelegramClient 
{ 
    // Это контракт, такое можно положить в папку Contracts и сделать только типы из этой папки публичными
    public interface IImageSender
    {
        Task SendDiagram(string chatId, byte[] photoData);
    }
}