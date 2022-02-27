using Discord.WebSocket;
using Discord.Commands;

using Microsoft.Extensions.Configuration;

using Victoria;

namespace DiscordMusicBot.Services {
    public class CommandHandler {
        private readonly DiscordSocketClient _discordSocketClient;
        public static CommandService? _commandService;
        private readonly IConfigurationRoot _iConfigurationRoot;
        private readonly IServiceProvider _iServiceProvider;
        private readonly LavaNode _lavaNode;

        public CommandHandler(DiscordSocketClient discordSocketClient, CommandService commandService, IConfigurationRoot iConfigurationRoot, IServiceProvider iServiceProvider, LavaNode lavaNode) {
            _discordSocketClient = discordSocketClient;
            _discordSocketClient.MessageReceived += OnMessageReceivedAsync;
            _discordSocketClient.Ready += OnReadyAsync;
            _commandService = commandService;

            _iConfigurationRoot = iConfigurationRoot;
            _iServiceProvider = iServiceProvider;

            _lavaNode = lavaNode;
        }

        private async Task OnReadyAsync() {
            if (_lavaNode != null && !_lavaNode.IsConnected) {
                await _lavaNode.ConnectAsync();
            }
        }

        private async Task OnMessageReceivedAsync(SocketMessage socketMessage) {
            if (socketMessage is not SocketUserMessage msg || msg.Author.Id == _discordSocketClient.CurrentUser.Id) {
                return;
            }

            var context = new SocketCommandContext(_discordSocketClient, msg);
            int argPos = 0;
            if (msg.HasStringPrefix(_iConfigurationRoot["prefix"], ref argPos) || msg.HasMentionPrefix(_discordSocketClient.CurrentUser, ref argPos)) {
                var result = await _commandService.ExecuteAsync(context, argPos, _iServiceProvider);

                if (!result.IsSuccess) {
                    await context.Channel.SendMessageAsync(result.ToString());
                }
            }
        }
    }
}
