using Codenames.Bot.Commands;
using Codenames.WordProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Codenames.Bot
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCodenames(this IServiceCollection services)
        {
            services.AddSingleton<IUsersRepostiory, SqlUsersRepository>();
            services.AddSingleton<GameFactory>();
            services.AddSingleton<GameUpdateHandler>();
            services.AddSingleton<GameManager>();
            services.AddSingleton<IChannelSender, ChannelSender>();
            services.AddSingleton<IGameScheduler>(sp =>
            {
                if (sp.GetRequiredService<IHostEnvironment>().IsProduction())
                    return new Scheduler();

                return new InteractiveScheduler();
            });
            services.AddSingleton<IWordsProvider>(sp =>
            {
                return new CacheWordsProvider(
                    new CacheWordsProviderSettings
                    {
                        UpdateInterval = TimeSpan.FromDays(1)
                    },
                    new CommaSeparetedHttpWordsProvider(new Uri(sp.GetRequiredService<BotSettings>().WordsUri))
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
                    new StopCommand(),
                    new SendMessageCommand()
                };
            });
            services.AddSingleton<TelegramBotClient>(sp => new TelegramBotClient(sp.GetRequiredService<BotSettings>().Token));
            services.AddDbContext<CodenamesDbContext>(x => x.UseSqlite("Data Source=codenames.db"), contextLifetime: ServiceLifetime.Singleton);

            return services;
        }
    }
}
