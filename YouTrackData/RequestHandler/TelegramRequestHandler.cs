using TelegramClient;

namespace YouTrackData
{
    public class TelegramRequestHandler: ITelegramRequestHandler
    {
        private readonly IBurndownService _burndownService;
        private readonly IDiagramGenerator _diagramGenerator;
        private readonly IImageSender _imageSender;
        public TelegramRequestHandler(IBurndownService burndownService, IDiagramGenerator diagramGenerator, IImageSender imageSender)
        {
            _burndownService = burndownService;
            _diagramGenerator = diagramGenerator;
            _imageSender = imageSender;
        }
        public async Task HandleRequest(string chatId)
        {
            BurndownData burndownData = await _burndownService.GetCurrentBurndown();

            byte[] png = _diagramGenerator.GenerateDiagram(burndownData);

            await _imageSender.SendDiagram(chatId, png);
        }
    }
}
