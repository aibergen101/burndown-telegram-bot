using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using TelegramClient;

namespace Scheduling
{
    public class DailyScheduler : BackgroundService
    {
        private readonly string _chatId;
        private readonly ILogger<DailyScheduler> _logger;
        private ITelegramRequestHandler _request;
        private static readonly TimeZoneInfo MoscowTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        public DailyScheduler(ITelegramRequestHandler request, string chatId, ILogger<DailyScheduler> logger)
        {
            _request = request;
            _chatId = chatId;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var nowMoscow = TimeZoneInfo.ConvertTime(nowUtc, MoscowTimeZone);

                var nextRun = new DateTime(
                    nowMoscow.Year,
                    nowMoscow.Month,
                    nowMoscow.Day,
                    11, 15, 0,
                    DateTimeKind.Unspecified);

                if (nowMoscow.DateTime >= nextRun)
                    nextRun = nextRun.AddDays(1);

                while (nextRun.DayOfWeek == DayOfWeek.Saturday || nextRun.DayOfWeek == DayOfWeek.Sunday)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(
                    nextRun, MoscowTimeZone);

                var delay = nextRunUtc - DateTime.UtcNow;

                _logger.LogInformation(
                    "Next diagram scheduled for {MoscowTime} Moscow time ({UtcTime} UTC)",
                    nextRun,
                    nextRunUtc);

                try
                {
                    await Task.Delay(delay, token);
                    await _request.HandleRequest(_chatId);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in time sending diagram");
                }
            }
        }
    }
}
