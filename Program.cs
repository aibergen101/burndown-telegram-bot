using Microsoft.Extensions.Configuration;
using Models;
using Scheduling;
using System.Net.Http.Headers;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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
                    Console.WriteLine($"Error in time sending diagram: {ex}");
                }
            }
        }
    }
}

namespace YouTrackData
{
    public class UpdateHandler
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly DiagramGenerator _diagramGenerator;

        public UpdateHandler(ITelegramBotClient telegramBot, DiagramGenerator diagramGenerator)
        {
            _telegramBot = telegramBot;
            _diagramGenerator = diagramGenerator;
        }

        public void Start()
        {
            if (_telegramBot is TelegramBotClient botClient)
            {
                botClient.OnMessage += OnMessage;
                botClient.OnUpdate += OnUpdate;
                botClient.OnError += OnError;
            }
        }
        public async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine(exception);
        }
        public async Task OnMessage(Message msg, UpdateType type)
        {
            // если будет больше 2 команд, разделить на отдельные методы, чтобы не перегружать метод OnMessage
            // с помощью создания интерфейса, который будет вызывать подходящие методы 
            if (msg.Text == "/start")
            {
                await _telegramBot.SendMessage(msg.Chat, "Press the button to see a diagram",
                    replyMarkup: new InlineKeyboardButton[] { "Sprint Diagram" });
            }
        }
        public async Task OnUpdate(Update update)
        {
            if (update.CallbackQuery is not { } query)
                return;

            if (query.Message is null)
                return;

            var chatId = query.Message.Chat.Id.ToString();

            try
            {
                await _telegramBot.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");
            }
            catch (ApiRequestException ex)
            {
                Console.WriteLine($"AnswerCallbackQuery failed (Id={query.Id}): {ex.Message}");
            }

            try
            {
                await _diagramGenerator.GenerateAndSendDiagram(chatId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await _telegramBot.SendMessage(chatId, "Не удалось построить диаграмму, попробуйте позже");
            }
        }
    }
    public class YouTrackClient
    {
        private readonly HttpClient _httpClient;
        public YouTrackClient(string youTrackToken)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", youTrackToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<Sprint> GetSprintDuration()
        {
            var sprintDuration = await _httpClient.GetAsync("https://acquirica.youtrack.cloud/api/agiles/176-23/sprints/current?fields=start,finish");
            var outputDuration = await sprintDuration.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Sprint>(outputDuration, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<Issue[]> GetSprintIssues()
        {
            var sprintName = Uri.EscapeDataString("Sprints: {2026.19T} Story points: *");
            var sprintStoryPoints = await _httpClient.GetAsync($"https://acquirica.youtrack.cloud/api/issues?fields=id,resolved,customFields(name,value)&customFields=Story%20points&query={sprintName}");
            var outputStoryPoints = await sprintStoryPoints.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Issue[]>(outputStoryPoints, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
    public class BurndownData
    {
        public double[] XDays { get; set; }
        public double[] YDays { get; set; }
        public double[] IdealLine { get; set; }
        public double[] ProgressLine { get; set; }
    }
    public class BurndownCalculator
    {
        public BurndownData CalculateBurndown(Sprint sprint, Issue[] issues)
        { 
            var startDate = DateTimeOffset.FromUnixTimeMilliseconds(sprint.Start).DateTime;
            var finishDate = DateTimeOffset.FromUnixTimeMilliseconds(sprint.Finish).DateTime;
    
            int totalStoryPoints = 0;
            foreach (var issue in issues)
            {
                totalStoryPoints += issue.StoryPoints;
            }

            int totalDays = (finishDate - startDate).Days;
            double totalPoints = totalStoryPoints;

            int currentDays = (DateTime.Now - startDate).Days;

            double[] progressLine = new double[currentDays + 1];
            progressLine[0] = totalStoryPoints;

            // Calculate the progress line based on resolved issues
            for (int day = 1; day <= currentDays; day++)
            {
                DateTime dayDate = startDate.AddDays(day);

                double doneSum = issues
                    .Where(iss => iss.Resolved.HasValue &&
                           DateTimeOffset.FromUnixTimeMilliseconds(iss.Resolved.Value).DateTime <= dayDate)
                    .Sum(iss => iss.StoryPoints);

                progressLine[day] = totalStoryPoints - doneSum;
            }

            double[] yDays = Enumerable.Range(0, currentDays + 1).Select(i => (double)i).ToArray();
            double[] xDays = Enumerable.Range(0, totalDays + 1).Select(i => (double)i).ToArray();
            double[] idealLine = new double[totalDays + 1];

            for (int i = 0; i <= totalDays; i++)
            {
                idealLine[i] = totalPoints - (totalPoints / totalDays) * i;
            }
            return new BurndownData
            {
                XDays = xDays,
                YDays = yDays,
                IdealLine = idealLine,
                ProgressLine = progressLine
            };
        }
    }
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

            // Diagram generation using ScottPlot
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
    public class ConfigReader
    {
        public static (string youTrackToken, string telegramToken, string chatId) ReadConfig()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var youTrackToken = config["YouTrackToken"]
                ?? throw new InvalidOperationException("YouTrackToken is not configured");
            var telegramToken = config["TelegramToken"]
                ?? throw new InvalidOperationException("TelegramToken is not configured");
            var chatId = config["ChatId"]
                ?? throw new InvalidOperationException("ChatId is not configured");

            return (youTrackToken, telegramToken, chatId);
        }
    }
}