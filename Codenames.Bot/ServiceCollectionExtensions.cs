using Codenames.Bot.Commands;
using Codenames.WordProviders;
using Microsoft.EntityFrameworkCore;
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
        public static IServiceCollection AddCodenames(this IServiceCollection services, BotSettings settings)
        {
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
                    new StopCommand(),
                    new SendMessageCommand()
                };
            });
            services.AddSingleton<TelegramBotClient>(x => new TelegramBotClient(settings.Token));
            services.AddDbContext<CodenamesDbContext>(x => x.UseSqlite("Data Source=codenames.db"));

            return services;
        }
    }
}
