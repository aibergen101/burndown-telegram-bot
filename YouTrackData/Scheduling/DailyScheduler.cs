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
                var now = DateTime.Now;
                var time = now.Date.AddHours(11).AddMinutes(15);
                if (now >= time) 
                    time = time.AddDays(1);

                while (time.DayOfWeek == DayOfWeek.Saturday || time.DayOfWeek == DayOfWeek.Sunday)
                {
                    time = time.AddDays(1);
                }

                try
                {
                    await Task.Delay(time - now, token);
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
