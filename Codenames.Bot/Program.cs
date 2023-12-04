
using Codenames;
using Codenames.Bot;
using Codenames.WordProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text.Json;
using Telegram.Bot;

try
{
    var settingsFileName = "settings.json";

    var settingsJson = File.ReadAllText(settingsFileName);

    var settings = JsonSerializer.Deserialize<BotSettings>(settingsJson)!;


    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru");

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IUsersRepostiory, SqlUsersRepository>();
    services.AddSingleton<GameFactory>();
    services.AddSingleton<GameUpdateHandler>();
    services.AddSingleton<GameManager>();
    services.AddSingleton<IWordsProvider>(sp =>
    {
        return new CacheWordsProvider(
            new CacheWordsProviderSettings
            {
                UpdateInterval = TimeSpan.FromDays(1)
            },
            new CommaSeparetedHttpWordsProvider(new Uri(settings.WordsUri))
        );
    });
    services.AddSingleton<GameFactorySettings>(new GameFactorySettings
    {
        TotalWords = 6,
        RiddleWords = 2
    });
    services.AddSingleton<IEnumerable<ICommand>>(sp =>
    {
        return new ICommand[]
        {
        new StartCommand(),
        new ShowWordsCommand(),
        new StopCommand()
        };
    });
    services.AddDbContext<CodenamesDbContext>(x => x.UseSqlite("Data Source=codenames.db"));


    var handler = services.BuildServiceProvider().GetRequiredService<GameUpdateHandler>();

    using CancellationTokenSource cts = new();

    var client = new TelegramBotClient(settings.Token);

    client.StartReceiving(
        updateHandler: handler,
        cancellationToken: cts.Token
    );

    handler.Run(client);

    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(60));
    }
} catch (Exception e)
{
    Console.WriteLine(e.Message);
}

