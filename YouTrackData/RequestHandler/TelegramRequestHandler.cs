using TelegramClient;
using Microsoft.Extensions.Logging;

namespace YouTrackData
{
    // Нет ли тут циклической зависимости? Зависимость интерфейсов выглядит странно - тут реализация, в инфраструктурном сервие - контракт (интерфейс)
    // Будто бы не в ту сторону зависимости
    public class TelegramRequestHandler: ITelegramRequestHandler
    {
        private readonly IBurndownService _burndownService;
        private readonly IDiagramGenerator _diagramGenerator;
        private readonly IImageSender _imageSender;
        private readonly ILogger<TelegramRequestHandler> _logger;
        public TelegramRequestHandler(IBurndownService burndownService, IDiagramGenerator diagramGenerator, IImageSender imageSender, ILogger<TelegramRequestHandler> logger)
        {
            _burndownService = burndownService;
            _diagramGenerator = diagramGenerator;
            _imageSender = imageSender;
            _logger = logger;
        }
        public async Task HandleRequest(string chatId, ChartTheme theme)
        {
            BurndownData burndownData = await _burndownService.GetCurrentBurndown();
            byte[] png = _diagramGenerator.GenerateDiagram(burndownData, theme);
            await _imageSender.SendDiagram(chatId, png);
        }
    }
}
