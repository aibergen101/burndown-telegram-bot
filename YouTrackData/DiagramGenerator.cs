using Telegram.Bot;

namespace YouTrackData
{
    public interface IDiagramGenerator
    {
        Task GenerateAndSendDiagram(string chatId);
    }

    public class DiagramGenerator : IDiagramGenerator
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly IYouTrackClient _youTrackClient;
        private readonly IBurndownCalculator _burndownCalculator;
        public DiagramGenerator(ITelegramBotClient telegramBot, IYouTrackClient youTrackClient, IBurndownCalculator burndownCalculator)
        {
            _telegramBot = telegramBot;
            _youTrackClient = youTrackClient;
            _burndownCalculator = burndownCalculator;
        }
        public async Task GenerateAndSendDiagram(string chatId)
        {
            var sprint = await _youTrackClient.GetSprintDuration();
            var issues = await _youTrackClient.GetSprintIssues();
            var burndownData = _burndownCalculator.CalculateBurndown(sprint, issues);

            ScottPlot.Plot myPlot = new();
            var idealScatter = myPlot.Add.Scatter(burndownData.XDays, burndownData.IdealLine);
            idealScatter.Color = ScottPlot.Colors.Green;
            idealScatter.LineStyle.Pattern = ScottPlot.LinePattern.Solid;

            var progressScatter = myPlot.Add.Scatter(burndownData.YDays, burndownData.ProgressLine);
            progressScatter.Color = ScottPlot.Colors.Blue;
            progressScatter.LineStyle.Pattern = ScottPlot.LinePattern.Solid;

            myPlot.SavePng("diagram.png", 400, 300);

            await using var photoStream = File.OpenRead("diagram.png");
            await _telegramBot.SendPhoto(chatId, Telegram.Bot.Types.InputFile.FromStream(photoStream, "diagram.png"));
        }
    }
}