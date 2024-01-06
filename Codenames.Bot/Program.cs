using Codenames.Bot;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru");

var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.ConfigureServices((app, services) =>
{
    services.AddSingleton<BotSettings>(app.Configuration.GetSection("Game").Get<BotSettings>()!);
    services.AddSingleton<ChannelSenderOptions>(app.Configuration.GetSection("Channel").Get<ChannelSenderOptions>()!);

    if (app.HostingEnvironment.IsProduction())
        services.AddLogging();
    services.AddCodenames();
    services.AddHostedService<CodenamesWorker>();
});

var host = hostBuilder.Build();

await host.RunAsync();