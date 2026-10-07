using Microsoft.Extensions.Configuration;

namespace YouTrackData
{
    public class ConfigReader
    {
        public static (string youTrackToken, string telegramToken, string chatId) ReadConfig()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var youTrackToken = config["YouTrackToken"]
                ?? throw new InvalidOperationException("YouTrackToken is not configured");
            var telegramToken = config["TelegramToken"]
                ?? throw new InvalidOperationException("TelegramToken is not configured");
            var chatId = config["ChatId"]
                ?? throw new InvalidOperationException("ChatId is not configured");

            return (youTrackToken, telegramToken, chatId);
        }
    }
}