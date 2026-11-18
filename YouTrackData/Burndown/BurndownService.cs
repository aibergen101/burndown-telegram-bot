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
            // Вот тут происходит смешение контрактов:
            // Ты из youtrackClient получил контракты, которые соответсвтуют API youtrack (не преобразовывя их никак, сразу в них десериализовал), затем передал их в основной сервис без преобразования.
            // В итоге у тебя получилось, что основное приложение напрямую зависит от API инфраструктуры.
            // Правильно будет использовать везде разные модели (даже если они похожи внешне) и маппить в них при передаче
            // См https://learn.microsoft.com/en-us/azure/architecture/patterns/anti-corruption-layer - просто примени не к микросервисам, а слоям приложения
            // Вообще даже лучше всего будет, если ты абстрагируешься от таск трекера, и у тебя будет интерфейс ITaskTracker, в котором будут нужные свойства, а то что это именно Youtrack - это деталь реализации
            var burndownData = _burndownCalculator.CalculateBurndown(sprint, issues);
            return burndownData;
        }
    }
}