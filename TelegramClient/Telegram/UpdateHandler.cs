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
            _logger.LogError(exception, "An error occurred");
        }
        public async Task OnMessage(Message msg, UpdateType type)
        {
            if (!_accessControl.IsAllowed(msg.From?.Id))   
            {
                _logger.LogWarning("Access denied for user {UserId}", msg.From?.Id);
                return;
            }
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

            if (!_accessControl.IsAllowed(query.From.Id))
            {
                _logger.LogWarning("Access denied for user {UserId}", query.From.Id);
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
                _logger.LogWarning(ex, "AnswerCallbackQuery failed (Id={QueryId})", query.Id);
            }

            try
            {
                await _handler.HandleRequest(chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate and send diagram for chat {ChatId}", chatId);
                await _telegramBot.SendMessage(chatId, "Could not generate diagram, please try again later");
            }
        }
    }
}