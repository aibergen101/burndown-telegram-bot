using Models;
using ScottPlot;
using Telegram.Bot;
using Telegram.Bot.Types;
using YouTrackData.Clients;

namespace YouTrackData
{
    public class BurndownService : IBurndownService
    {
        private readonly IYouTrackClient _youTrackClient;
        private readonly IBurndownCalculator _burndownCalculator;
        public BurndownService(IYouTrackClient youTrackClient, IBurndownCalculator burndownCalculator)
        {
            _youTrackClient = youTrackClient;
            _burndownCalculator = burndownCalculator;
        }
        public async Task<BurndownData> GetCurrentBurndown()
        {
            var sprint = await _youTrackClient.GetSprint();
            var issues = await _youTrackClient.GetSprintIssues();
            var burndownData = _burndownCalculator.CalculateBurndown(sprint, issues);
            return burndownData;
        }
    }
}