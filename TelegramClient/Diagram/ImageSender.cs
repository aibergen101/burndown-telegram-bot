using Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace TelegramClient
{
    public class ImageSender : IImageSender
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly ILogger<ImageSender> _logger;
        public ImageSender(ITelegramBotClient telegramBot, ILogger<ImageSender> logger)
        {
            _telegramBot = telegramBot;
            _logger = logger;   
        }
        public async Task SendDiagram(string chatId, byte[] photoData)
        {
            _logger.LogInformation("Sending diagram to chat {chatId}", chatId);
            await _telegramBot.SendPhoto(chatId, Telegram.Bot.Types.InputFile.FromStream(new MemoryStream(photoData), "diagram.png"));
        }
    }
}