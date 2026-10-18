using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scheduling;
using Telegram.Bot;
using TelegramClient;
using YouTrackData;
using YouTrackData.Clients;

var (youTrackToken, telegramToken, chatId, allowedIds) = ConfigReader.ReadConfig();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IImageSender, ImageSender>();
builder.Services.AddSingleton<ITelegramRequestHandler, TelegramRequestHandler>();
builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddSingleton<IYouTrackClient>(sp =>
    new YouTrackClient(youTrackToken, sp.GetRequiredService<ILogger<YouTrackClient>>()));
builder.Services.AddSingleton<IBurndownCalculator, BurndownCalculator>();
builder.Services.AddSingleton<IBurndownService, BurndownService>();
builder.Services.AddSingleton<IDiagramGenerator, DiagramGenerator>();

builder.Services.AddSingleton(_ => new TelegramBotClient(telegramToken));
builder.Services.AddSingleton<ITelegramBotClient>(sp => sp.GetRequiredService<TelegramBotClient>());

builder.Services.AddHostedService(sp => new DailyScheduler(
    sp.GetRequiredService<ITelegramRequestHandler>(),
    chatId,
    sp.GetRequiredService<ILogger<DailyScheduler>>()));

builder.Services.AddSingleton<IAccessControl>(new AccessControl(allowedIds));

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

host.Services.GetRequiredService<UpdateHandler>().Start();
logger.LogInformation("Bot is running");
host.Run();

