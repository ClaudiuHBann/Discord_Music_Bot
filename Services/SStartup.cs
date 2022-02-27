using Discord.WebSocket;
using Discord.Commands;
using Discord;

using Microsoft.Extensions.Configuration;

using System.Reflection;

namespace DiscordMusicBot.Services {
    public class SStartup {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly CommandService _commandService;

        private readonly IServiceProvider _iServiceProvider;
        private readonly IConfigurationRoot _iConfigurationRoot;

        public SStartup(IServiceProvider iServiceProvider, DiscordSocketClient discordSocketClient, CommandService commandService, IConfigurationRoot iConfigurationRoot) {
            _discordSocketClient = discordSocketClient;
            _commandService = commandService;

            _iServiceProvider = iServiceProvider;
            _iConfigurationRoot = iConfigurationRoot;
        }

        public async Task StartAsync() {
            if (string.IsNullOrWhiteSpace(_iConfigurationRoot["tokens:discord"])) {
                throw new Exception("Please enter your bot's token into the `_configuration.json` file found in the applications root directory.");
            }

            await _discordSocketClient.LoginAsync(TokenType.Bot, _iConfigurationRoot["tokens:discord"]);
            await _discordSocketClient.StartAsync();

            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _iServiceProvider);
        }
    }
}
