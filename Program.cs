using Scheduling;
using Telegram.Bot;
using YouTrackData;

var (youTrackToken, telegramToken, chatId) = ConfigReader.ReadConfig();

using var cts = new CancellationTokenSource();
var telegramBot = new TelegramBotClient(telegramToken, cancellationToken: cts.Token);
var youTrackClient = new YouTrackClient(youTrackToken);
var diagramGenerator = new DiagramGenerator(telegramBot,youTrackClient);
var updateHandler = new UpdateHandler(telegramBot, diagramGenerator);

updateHandler.Start();
var scheduler = new DailyScheduler(diagramGenerator, chatId);
var schedulerTask = scheduler.Scheduler(cts.Token);

Console.WriteLine("Bot is running");
await Task.Delay(-1);
cts.Cancel();