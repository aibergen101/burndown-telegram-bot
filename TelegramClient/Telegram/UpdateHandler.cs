using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;

namespace TelegramClient
{
    public class UpdateHandler
    {
        private readonly TelegramBotClient _telegramBot;
        private readonly ILogger<UpdateHandler> _logger;
        private readonly ITelegramRequestHandler _handler;
        private readonly IAccessControl _accessControl;
        public UpdateHandler(TelegramBotClient telegramBot, ITelegramRequestHandler handler, ILogger<UpdateHandler> logger, IAccessControl accessControl)
        {
            _telegramBot = telegramBot;
            _handler = handler;
            _accessControl = accessControl;
            _logger = logger;
        }

        public void Start()
        {
            _telegramBot.OnMessage += OnMessage;
            _telegramBot.OnUpdate += OnUpdate;
            _telegramBot.OnError += OnError;
        }
        public async Task OnError(Exception exception, HandleErrorSource source)
        {
            _logger.LogError(exception, "Произошла ошибка");
        }
        public async Task OnMessage(Message msg, UpdateType type)
        {
            if (!_accessControl.IsAllowed(msg.From?.Id))   
            {
                _logger.LogWarning("Доступ отклонен для пользователя {UserId}", msg.From?.Id);
                return;
            }
            if (msg.Text == "/start")
            {
                await _telegramBot.SendMessage(msg.Chat, "Нажми кнопку ниже, и я пришлю диаграмму по текущим данным спринта.",
                    replyMarkup: new InlineKeyboardButton[] { "📊 Получить диаграмму" });
            }
            else if(msg.Text == "/help")
            {
                await _telegramBot.SendMessage(msg.Chat, "🤖 Бот спринта\nЯ строю диаграмму по текущим данным спринта.\n\nЧто умею:\n/start: главное меню с кнопкой\n/help: справку");
            }
            else
            {
                await _telegramBot.SendMessage(msg.Chat, "Я понимаю только команды. Нажми /start или /help");
            }
        }
        public async Task OnUpdate(Update update)
        {
            if (update.CallbackQuery is not { } query)
                return;

            if (!_accessControl.IsAllowed(query.From.Id))
            {
                _logger.LogWarning("Доступ отклонен для пользователя {UserId}", query.From.Id);
                await _telegramBot.AnswerCallbackQuery(query.Id, "No access");
                return;
            }

            if (query.Message is null)
                return;

            var chatId = query.Message.Chat.Id.ToString();

            try
            {
                await _telegramBot.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");
            }
            catch (ApiRequestException ex)
            {
                _logger.LogWarning(ex, "Сбой при выполнении AnswerCallbackQuery (Id={QueryId})", query.Id);
            }
            if (query.Data == "📊 Получить диаграмму")
            {
                await _telegramBot.SendMessage(chatId, "Выбери тему диаграммы:",
                    replyMarkup: new InlineKeyboardButton[] { "☀️ Светлая", "🌙 Тёмная" });
            }
            else if (query.Data == "☀️ Светлая" || query.Data == "🌙 Тёмная")
            {
                var theme = query.Data == "☀️ Светлая" ? ChartTheme.Light : ChartTheme.Dark;

                try
                {
                    await _handler.HandleRequest(chatId.ToString(), theme);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось создать и отправить диаграмму для чата {ChatId}", chatId);
                    await _telegramBot.SendMessage(chatId, "Не удалось создать диаграмму, пожалуйста, попробуйте позже");
                }
            }
        }
    }
}