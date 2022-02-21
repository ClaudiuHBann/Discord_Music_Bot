using Discord.WebSocket;
using Discord.Commands;
using Discord;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

using Victoria;

namespace DiscordMusicBot {
    public class Program {
        private static DiscordSocketClient? _sharedClient = null;
        private static CommandService? _commands = null;
        private IServiceProvider? _services = null;
        private DiscordSocketConfig? _config = null;
        private static LavaNode? _lavaNode = null;

        static void Main() {
            new Program().RunAsync().GetAwaiter().GetResult();
        }

        public static void SetLavaNode(LavaNode? lavaNode) {
            _lavaNode = lavaNode;
        }

        public static CommandService GetCommandService() {
            return _commands;
        }

        public async Task RunAsync() {
            _config = new DiscordSocketConfig {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 100
            };

            _sharedClient = new DiscordSocketClient(_config);
            _commands = new CommandService();

            _sharedClient.Ready += OnReadyAsync;

            _services = new ServiceCollection()
                .AddSingleton(_sharedClient)
                .AddSingleton(_commands)

                .AddLavaNode()

                .BuildServiceProvider();

            _sharedClient.Log += ClientLog;

            await RegisterCommandsAsync();

            await _sharedClient.LoginAsync(TokenType.Bot, "");
            await _sharedClient.StartAsync();

            await Task.Delay(-1);
        }

        private async Task OnReadyAsync() {
            if (_lavaNode != null && !_lavaNode.IsConnected) {
                await _lavaNode.ConnectAsync();
            }
        }

        private Task ClientLog(LogMessage arg) {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private async Task RegisterCommandsAsync() {
            if (_sharedClient != null && _commands != null) {
                _sharedClient.MessageReceived += HandleCommandAsync;
            }

            if (_commands != null) {
                await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            }
        }

        private async Task HandleCommandAsync(SocketMessage arg) {
            if (arg is not SocketUserMessage message || message.Author.IsBot) {
                return;
            }

            int argPos = 0;
            var context = new SocketCommandContext(_sharedClient, message);
            if (_commands != null && message.HasStringPrefix("!", ref argPos)) {
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess) {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }
    }
}