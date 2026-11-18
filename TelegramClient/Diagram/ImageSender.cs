using Telegram.Bot;
using Microsoft.Extensions.Logging;

// В целом лучше использовать file-scoped namespace
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
        // Есть расхождение в терминологии - image/photo. Надо выбрать, как мы это называем, и везде придерживаться единого термина
        public async Task SendDiagram(string chatId, byte[] photoData)
        {
            // логи лучше пишать на английском
            _logger.LogInformation("Отправляю диаграмму в чат {chatId}", chatId);
            // MemoryStream не надо dispos-ить, но хорошей практикой будет все-таки любой стрим оборачивать в using (заодно почитать что такое Dispose в .NET)
            await _telegramBot.SendPhoto(chatId, Telegram.Bot.Types.InputFile.FromStream(new MemoryStream(photoData), "diagram.png"));
        }
    }
}