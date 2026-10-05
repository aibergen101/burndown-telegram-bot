using Serilog;
using YouTrackData;

namespace Scheduling
{
    public class DailyScheduler
    {
        private readonly DiagramGenerator _diagramGenerator;
        private readonly string _chatId;
        public DailyScheduler(DiagramGenerator diagramGenerator, string chatId)
        {
            _chatId = chatId;
            _diagramGenerator = diagramGenerator;
        }
        public async Task Scheduler(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var time = now.Date.AddHours(10).AddMinutes(14);
                if (now >= time) time = time.AddDays(1);

                try
                {
                    await Task.Delay(time - now, token);
                    await _diagramGenerator.GenerateAndSendDiagram(_chatId);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in time sending diagram");
                }
            }
        }
    }
}
