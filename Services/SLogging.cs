using Discord.WebSocket;
using Discord.Commands;
using Discord;

namespace DiscordMusicBot.Services {
    public class SLogging {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly CommandService _commandService;

        private string _logsDirectory { get; }
        private string _logsFile => Path.Combine(_logsDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        public SLogging(DiscordSocketClient discordSocketClient, CommandService commandService) {
            _logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            _discordSocketClient = discordSocketClient;
            _discordSocketClient.Log += OnLogAsync;

            _commandService = commandService;
            _commandService.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage logMessage) {
            if (!Directory.Exists(_logsDirectory)) {
                Directory.CreateDirectory(_logsDirectory);
            }

            if (!File.Exists(_logsFile)) {
                File.Create(_logsFile).Dispose();
            }

            string log = $"{DateTime.UtcNow:hh:mm:ss} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Exception?.ToString() ?? logMessage.Message}";
            File.AppendAllText(_logsFile, log + "\n");

            return Console.Out.WriteLineAsync(log);
        }
    }
}
