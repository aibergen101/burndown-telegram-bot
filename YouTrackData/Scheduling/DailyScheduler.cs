using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using TelegramClient;

namespace Scheduling
{
    public class DailyScheduler : BackgroundService
    {
        private readonly string _chatId;
        private readonly ILogger<DailyScheduler> _logger;
        // это не request, а requestHandler 
        private ITelegramRequestHandler _request;
        private static readonly TimeZoneInfo MoscowTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        
        // Тут тоже, инжектить надо класс, конфиг, а не просто строку
        public DailyScheduler(ITelegramRequestHandler request, string chatId, ILogger<DailyScheduler> logger)
        {
            _request = request;
            _chatId = chatId;
            _logger = logger;
        }
        // между методами лучше все-таки оставлять пустые строки
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
                    "Следующая диаграмма будет отправлена {MoscowTime} по Москве ({UtcTime} UTC)",
                    nextRun,
                    nextRunUtc);

                try
                {
                    await Task.Delay(delay, token);
                    await _request.HandleRequest(_chatId,ChartTheme.Dark);
                }
                catch (OperationCanceledException)
                {
                    //  перехватывает исключения не только от Task.Delay, но и от HandleRequest - то есть при срабатывании, например если HttpClient остановится по таймауту, рассылка может остановиться навсегда
                    //  catch (OperationCanceledException) when (token.IsCancellationRequested) - так будет правильнее
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке диаграммы");
                }
            }
            
            // Можно проще написать, типа такого:
            
            //         while (!token.IsCancellationRequested)
            // {
            //     var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, Moscow);
            //     var next = now.Date + RunAt;
            //     if (next <= now) next = next.AddDays(1);
            //     while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) next = next.AddDays(1);

            //     logger.LogInformation("Следующая диаграмма: {Next} МСК", next);

            //     try
            //     {
            //         await Task.Delay(next - now, token);
            //         await handler.HandleRequest(chatId, ChartTheme.Dark);
            //     }
            //     catch (OperationCanceledException) when (token.IsCancellationRequested)
            //     {
            //         break;
            //     }
            //     catch (Exception ex)
            //     {
            //         logger.LogError(ex, "Ошибка при отправке диаграммы");
            //     }
            // }
        }
    }
}
