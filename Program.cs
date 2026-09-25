using Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var json = File.ReadAllText("appsettings.json");
var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
var botToken = config["TelegramToken"];

//telegram bot client
using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(botToken, cancellationToken: cts.Token);
bot.OnMessage += OnMessage;
bot.OnUpdate += OnUpdate;
bot.OnError += OnError;
Console.WriteLine("Bot is running");
await Task.Delay(-1);
cts.Cancel();

async Task OnError(Exception exception, HandleErrorSource source)
{
    Console.WriteLine(exception); 
}
async Task OnMessage(Message msg, UpdateType type)
{
    if (msg.Text == "/start")
    {
        await bot.SendMessage(msg.Chat, "Press the button to see a diagram",
            replyMarkup: new InlineKeyboardButton[] { "Sprint Diagram" });
    }
}
async Task OnUpdate(Update update)
{
    if (update.CallbackQuery is not null)
    {
        var query = update.CallbackQuery;
        await bot.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");
        await GenerateAndSendDiagram(query.Message!.Chat.Id.ToString());
    }
}
async Task GenerateAndSendDiagram(string chatId) {
    var json = File.ReadAllText("appsettings.json");
    var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    var token = config["YouTrackToken"];
    var botToken = config["TelegramToken"];

    using var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));

    var response = await client.GetAsync("https://acquirica.youtrack.cloud/api/agiles/176-23/sprints/current?fields=start,finish");
    var output = await response.Content.ReadAsStringAsync();

    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var sprint = JsonSerializer.Deserialize<Sprint>(output, options);

    var startD = DateTimeOffset.FromUnixTimeMilliseconds(sprint.Start).DateTime;
    var finishD = DateTimeOffset.FromUnixTimeMilliseconds(sprint.Finish).DateTime;
    var range = finishD - startD;

    var sprintName = "Sprints: {2026.19T} Story points: *";
    var Sprint = Uri.EscapeDataString(sprintName);
    var responseSP = await client.GetAsync($"https://acquirica.youtrack.cloud/api/issues?fields=id,resolved,customFields(name,value)&customFields=Story%20points&query={Sprint}");
    var outputSP = await responseSP.Content.ReadAsStringAsync();
    var issues = JsonSerializer.Deserialize<Issue[]>(outputSP, options);

    int storyPoints = 0;
    int totalStoryPoints = 0;

    foreach (var issue in issues)
    {
        totalStoryPoints += issue.StoryPoints;
    }

    int totalDays = (finishD - startD).Days;
    double totalPoints = totalStoryPoints;

    int currentDays = (DateTime.Now - startD).Days;
    
    double[] progressLine = new double[currentDays + 1];
    progressLine[0] = totalStoryPoints;

    for (int day = 1; day <= currentDays; day++)
    {
        DateTime dayDate = startD.AddDays(day);

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

    ScottPlot.Plot myPlot = new();
    var idealScatter = myPlot.Add.Scatter(xDays, idealLine);
    idealScatter.Color = ScottPlot.Colors.Green;
    idealScatter.LineStyle.Pattern = ScottPlot.LinePattern.Solid;

    var progressScatter = myPlot.Add.Scatter(yDays, progressLine);
    progressScatter.Color = ScottPlot.Colors.Blue;
    progressScatter.LineStyle.Pattern = ScottPlot.LinePattern.Solid;

    myPlot.SavePng("diagram.png", 400, 300);

    await using var photoStream = File.OpenRead("diagram.png");
    await bot.SendPhoto(chatId, Telegram.Bot.Types.InputFile.FromStream(photoStream, "diagram.png"));
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