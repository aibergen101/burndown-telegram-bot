using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace YouTrackData
{
    public class UpdateHandler
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly DiagramGenerator _diagramGenerator;

        public UpdateHandler(ITelegramBotClient telegramBot, DiagramGenerator diagramGenerator)
        {
            _telegramBot = telegramBot;
            _diagramGenerator = diagramGenerator;
        }

        public void Start()
        {
            if (_telegramBot is TelegramBotClient botClient)
            {
                botClient.OnMessage += OnMessage;
                botClient.OnUpdate += OnUpdate;
                botClient.OnError += OnError;
            }
        }
        public async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine(exception);
        }
        public async Task OnMessage(Message msg, UpdateType type)
        {
            // если будет больше 2 команд, разделить на отдельные методы, чтобы не перегружать метод OnMessage
            // с помощью создания интерфейса, который будет вызывать подходящие методы 
            if (msg.Text == "/start")
            {
                await _telegramBot.SendMessage(msg.Chat, "Press the button to see a diagram",
                    replyMarkup: new InlineKeyboardButton[] { "Sprint Diagram" });
            }
        }
        public async Task OnUpdate(Update update)
        {
            if (update.CallbackQuery is not { } query)
                return;

            if (query.Message is null)
                return;

            var chatId = query.Message.Chat.Id.ToString();

            try
            {
                await _telegramBot.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");
            }
            catch (ApiRequestException ex)
            {
                Console.WriteLine($"AnswerCallbackQuery failed (Id={query.Id}): {ex.Message}");
            }

            try
            {
                await _diagramGenerator.GenerateAndSendDiagram(chatId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await _telegramBot.SendMessage(chatId, "Не удалось построить диаграмму, попробуйте позже");
            }
        }
    }
}