using Codenames.Bot;
using System.Globalization;
using System.Text.Json;

var settingsFileName = "settings.json";
var settingsJson = File.ReadAllText(settingsFileName);
var settings = JsonSerializer.Deserialize<BotSettings>(settingsJson)!;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru");

var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.ConfigureServices(services =>
{
    services.AddLogging();
    services.AddCodenames(settings);
    services.AddHostedService<CodenamesWorker>();
});

var host = hostBuilder.Build();
await host.RunAsync();