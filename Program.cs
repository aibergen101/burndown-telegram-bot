using Microsoft.Extensions.Configuration;
using Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YouTrackData;


var (youTrackToken, telegramToken, chatId) = UpdateHandler.ReadConfig();

// Create a cancellation token source to handle graceful shutdown 
using var cts = new CancellationTokenSource();
var telegramBot = new TelegramBotClient(telegramToken, cancellationToken: cts.Token);
var handler =new UpdateHandler(telegramBot,youTrackToken);
handler.Start();

var schedulerTask = Scheduler(handler, chatId, youTrackToken, cts.Token);

Console.WriteLine("Bot is running");
await Task.Delay(-1);
cts.Cancel();

static async Task Scheduler(UpdateHandler handler, string chatId, string youTrackToken, CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        var now = DateTime.Now;
        var time = now.Date.AddHours(10).AddMinutes(10);
        if (now >= time) time = time.AddDays(1);

        try
        {
            await Task.Delay(time - now, token);
            await handler.GenerateAndSendDiagram(chatId, youTrackToken);
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

namespace YouTrackData
{
    //разделить на отдельные классы, так как класс выполняет несколько функций:
    //обработка обновлений, генерация диаграммы, чтение конфигурации
    public class UpdateHandler
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly string _youTrackToken;

        public UpdateHandler(ITelegramBotClient telegramBot, string youTrackToken)
        {
            _telegramBot = telegramBot;
            _youTrackToken = youTrackToken;
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
                // Callback устарел или его уже обработал другой экземпляр бота.
                // Не критично: диаграмму всё равно отправляем.
                Console.WriteLine($"AnswerCallbackQuery failed (Id={query.Id}): {ex.Message}");
            }

            try
            {
                await GenerateAndSendDiagram(chatId, _youTrackToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await _telegramBot.SendMessage(chatId, "Не удалось построить диаграмму, попробуйте позже");
            }
        }
        public async Task GenerateAndSendDiagram(string chatId, string youTrackToken)
        {
            // Create HttpClient to interact with YouTrack API
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", youTrackToken);
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // Get sprint duration
            var sprintDuration = await httpClient.GetAsync("https://acquirica.youtrack.cloud/api/agiles/176-23/sprints/current?fields=start,finish");
            var outputDuration = await sprintDuration.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sprintDate = JsonSerializer.Deserialize<Sprint>(outputDuration, options);

            var startDate = DateTimeOffset.FromUnixTimeMilliseconds(sprintDate.Start).DateTime;
            var finishDate = DateTimeOffset.FromUnixTimeMilliseconds(sprintDate.Finish).DateTime;
            var sprintRange = finishDate - startDate;

            // Get issues in the sprint and their story points
            var sprintData = "Sprints: {2026.19T} Story points: *";
            var sprintName = Uri.EscapeDataString(sprintData);
            var sprintStoryPoints = await httpClient.GetAsync($"https://acquirica.youtrack.cloud/api/issues?fields=id,resolved,customFields(name,value)&customFields=Story%20points&query={sprintName}");
            var outputStoryPoints = await sprintStoryPoints.Content.ReadAsStringAsync();
            var issues = JsonSerializer.Deserialize<Issue[]>(outputStoryPoints, options);

            // Calculate total story points
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

            // Diagram generation using ScottPlot
            ScottPlot.Plot myPlot = new();
            var idealScatter = myPlot.Add.Scatter(xDays, idealLine);
            idealScatter.Color = ScottPlot.Colors.Green;
            idealScatter.LineStyle.Pattern = ScottPlot.LinePattern.Solid;

            var progressScatter = myPlot.Add.Scatter(yDays, progressLine);
            progressScatter.Color = ScottPlot.Colors.Blue;
            progressScatter.LineStyle.Pattern = ScottPlot.LinePattern.Solid;

            myPlot.SavePng("diagram.png", 400, 300);

            await using var photoStream = File.OpenRead("diagram.png");
            await _telegramBot.SendPhoto(chatId, Telegram.Bot.Types.InputFile.FromStream(photoStream, "diagram.png"));
        }

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
    //telegram send 
    //var photo = @"diagram.png";
    //var url = $"https://api.telegram.org/bot{botToken}/sendPhoto";

    //using var content = new MultipartFormDataContent();
    //content.Add(new StringContent(chatId), "chat_id");

    //var fileBytes = await File.ReadAllBytesAsync(photo);
    //var fileContent = new ByteArrayContent(fileBytes);

    //fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
    //content.Add(fileContent, "photo", Path.GetFileName(photo));

    //var tgResponse = await client.PostAsync(url, content);
    //var tgOutput = await tgResponse.Content.ReadAsStringAsync();
}
