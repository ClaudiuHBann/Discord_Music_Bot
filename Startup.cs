using Discord.WebSocket;
using Discord.Commands;
using Discord;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using DiscordMusicBot.Services;

using Victoria;

namespace DiscordMusicBot {
    public class Startup {
        public IConfigurationRoot IConfigurationRoot { get; }

        public Startup(string[] args) {
            IConfigurationRoot = new ConfigurationBuilder()
                                .SetBasePath(AppContext.BaseDirectory)
                                .AddJsonFile("_configuration.json")
                                .Build();
        }

        public static async Task RunAsync(string[] args) => await new Startup(args).RunAsync();

        public async Task RunAsync() {
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<SLogging>();
            provider.GetRequiredService<CommandHandler>();
            await provider.GetRequiredService<SStartup>().StartAsync();

            await Task.Delay(-1);
        }

        private void ConfigureServices(IServiceCollection iServiceCollection) {
            iServiceCollection.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
            }))
            .AddSingleton<CommandHandler>()
            .AddSingleton<SStartup>()
            .AddSingleton<SLogging>()
            .AddLavaNode(lavaConfig => {
                // lavaConfig.IsSsl = true;
                // lavaConfig.LogSeverity = LogSeverity.Verbose;
            })
            .AddSingleton(IConfigurationRoot);
        }
    }
}
