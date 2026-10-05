using Telegram.Bot;

namespace YouTrackData
{
    public class DiagramGenerator
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly YouTrackClient _youTrackClient;
        private readonly BurndownCalculator _burndownCalculator = new();
        public DiagramGenerator(ITelegramBotClient telegramBot, YouTrackClient youTrackClient)
        {
            _telegramBot = telegramBot;
            _youTrackClient = youTrackClient;
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