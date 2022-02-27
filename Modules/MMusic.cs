using Discord.Commands;
using Discord;

using Victoria.EventArgs;
using Victoria;

using System.Text.RegularExpressions;

namespace DiscordMusicBot.Modules {
    public class MMusic : ModuleBase<SocketCommandContext> {
        private readonly LavaNode _lavaNode;
        private static bool repeat = false;

        public MMusic(LavaNode lavaNode) {
            _lavaNode = lavaNode;
            _lavaNode.OnTrackEnded += OnTrackEndedAsync;
        }

        private async Task OnTrackEndedAsync(TrackEndedEventArgs arg) {
            if (arg.Player.PlayerState == Victoria.Enums.PlayerState.Stopped) {
                if (repeat) {
                    await arg.Player.PlayAsync(arg.Track);
                } else {
                    if (arg.Player.Queue != null && arg.Player.Queue.Count > 0) {
                        await arg.Player.SkipAsync();
                        await arg.Player.TextChannel.SendMessageAsync("Now Playing...", false, await CreateEmbed(arg.Player.Track));
                    }
                }
            }
        }

        [Command("join")]
        public async Task JoinAsync() {
            if (!await IsConnectedUser()) {
                return;
            }

            if (_lavaNode.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            await _lavaNode.JoinAsync(voiceState?.VoiceChannel, Context.Channel as ITextChannel);
            await ReplyAsync($"Joined the '{voiceState?.VoiceChannel.Name}' voice channel!");
        }

        [Command("play")]
        public async Task PlayAsync([Remainder] string query) {
            if (string.IsNullOrWhiteSpace(query)) {
                await ReplyAsync("Please provide search terms.");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild)) {
                await JoinAsync();
            }

            var searchResponse = await _lavaNode.SearchYouTubeAsync(query);
            if (searchResponse.Status == Victoria.Responses.Search.SearchStatus.LoadFailed ||
                searchResponse.Status == Victoria.Responses.Search.SearchStatus.NoMatches) {
                await ReplyAsync($"I wasn't able to find anything for '{query}'.");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            var track = searchResponse.Tracks.ToArray()[0];
            if (player.PlayerState == Victoria.Enums.PlayerState.Playing ||
                player.PlayerState == Victoria.Enums.PlayerState.Paused) {
                player.Queue.Enqueue(track);
                await ReplyAsync($"Enqueued: {track.Title}");
            } else {
                await player.PlayAsync(track);
                await ReplyAsync(null, false, await CreateEmbed(track));
            }
        }

        [Command("skip")]
        public async Task SkipAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (player.Queue.Count == 0) {
                await player.StopAsync();
                return;
            }

            await player.SkipAsync();
            await ReplyAsync(null, false, await CreateEmbed(player.Track));
        }

        [Command("pause")]
        public async Task PauseAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (player.PlayerState == Victoria.Enums.PlayerState.Paused ||
                player.PlayerState == Victoria.Enums.PlayerState.Stopped) {
                await ReplyAsync("The music is already paused!");
                return;
            }

            await player.PauseAsync();
            await ReplyAsync("Paused the music!");
        }

        [Command("resume")]
        public async Task ResumeAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (player.PlayerState == Victoria.Enums.PlayerState.Playing) {
                await ReplyAsync("The music is already playing!");
                return;
            }

            await player.ResumeAsync();
            await ReplyAsync("Resumed the music!");
        }






        [Command("stop")]
        public async Task StopAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (player.PlayerState != Victoria.Enums.PlayerState.Playing) {
                await ReplyAsync("No music playing!");
                return;
            }

            await player.StopAsync();
            await ReplyAsync("Stopped the music!");
        }

        [Command("leave")]
        public async Task LeaveAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            var voiceState = Context.User as IVoiceState;
            await _lavaNode.LeaveAsync(voiceState?.VoiceChannel);
        }

        [Command("clear")]
        [Alias("clr", "jump")]
        public async Task ClearAsync([Remainder] string fromTo) {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (string.IsNullOrEmpty(fromTo)) {
                player.Queue.Clear();
            }

            if (Regex.IsMatch(fromTo, "[0-9 ]+")) {
                string[] numbers = fromTo.Split();

                int from = int.Parse(numbers[0]);
                if (from > player.Queue.Count) {
                    await ReplyAsync($"There are not {from} tracks in the queue.");
                    return;
                }

                if (numbers.Length > 1) {
                    int to = int.Parse(numbers[1]);

                    if (to - from > player.Queue.Count) {
                        await ReplyAsync($"There are not {to - from} tracks in the queue.");
                    } else {
                        if (to > player.Queue.Count) {
                            await ReplyAsync($"There are not {to} tracks in the queue.");
                            return;
                        }

                        if (from > to) {
                            await ReplyAsync($"From value({from}) is bigger than to value({to}).");
                            return;
                        }

                        player.Queue.RemoveRange(from, to);
                    }
                } else {
                    player.Queue.RemoveRange(0, from);
                }
            } else {
                await ReplyAsync("The command is wrong!");
            }
        }

        [Command("repeat")]
        public async Task RepeatAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            if (repeat = !repeat) {
                await ReplyAsync("I am going to repeat...", false, await CreateEmbed(_lavaNode.GetPlayer(Context.Guild).Track));
            } else {
                await ReplyAsync("I have stopped repeating...", false, await CreateEmbed(_lavaNode.GetPlayer(Context.Guild).Track));
            }
        }

        [Command("shuffle")]
        public async Task ShuffleAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.GetPlayer(Context.Guild).Queue.Shuffle();
        }

        [Command("seek")]
        public async Task SeekAsync(int hours, int minutes, int seconds) {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            if (_lavaNode.GetPlayer(Context.Guild).Track.CanSeek) {
                await _lavaNode.GetPlayer(Context.Guild).SeekAsync(new TimeSpan(hours, minutes, seconds));
            }
        }

        [Command("np")]
        public async Task NPAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            await ReplyAsync(_lavaNode.GetPlayer(Context.Guild).Track.Position.ToString(), false, await CreateEmbed(_lavaNode.GetPlayer(Context.Guild).Track));
        }

        [Command("search")]
        public async Task SearchAsync([Remainder] string query) {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            await _lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.YouTube, query).ContinueWith(async (searchResponse) => {
                string tracks = "";

                foreach (var item in searchResponse.Result.Tracks) {
                    tracks += item.Title + '\n';
                }

                await ReplyAsync("Tracks found on youtube:", false, new EmbedBuilder()
                    .WithColor(Color.DarkBlue)
                    .WithDescription(tracks)
                    .WithFooter(footer => footer.Text = "Pinn is the Best!")
                    .Build());
            });
        }

        [Command("lyrics")]
        public async Task LyricsAsync() {
            var track = _lavaNode.GetPlayer(Context.Guild).Track;

            string lyricsFromGenius = track.FetchLyricsFromGeniusAsync().GetAwaiter().GetResult();
            if (lyricsFromGenius != null && lyricsFromGenius != "") {
                await ReplyAsync(null, false, new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle(track.Title)
                .WithDescription(lyricsFromGenius)
                .WithThumbnailUrl(await track.FetchArtworkAsync())
                .WithFooter(footer => footer.Text = "Pinn is the Best!")
                .Build());
            } else {
                string lyricsFromOvh = track.FetchLyricsFromOvhAsync().GetAwaiter().GetResult();
                if (lyricsFromOvh != null && lyricsFromOvh != "") {
                    await ReplyAsync(null, false, new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle(track.Title)
                .WithDescription(lyricsFromOvh)
                .WithThumbnailUrl(await track.FetchArtworkAsync())
                .WithFooter(footer => footer.Text = "Pinn is the Best!")
                .Build());
                } else {
                    await ReplyAsync("No lyrics found for " + _lavaNode.GetPlayer(Context.Guild).Track.Title);
                }
            }
        }

        [Command("queue")]
        [Alias("q")]
        public async Task QueueAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            string q = "";
            var queue = _lavaNode.GetPlayer(Context.Guild).Queue.ToArray();
            for (int i = 1; i < queue.Length; i++) {
                q += i + ". " + queue[i].Title + "\n";
            }

            await ReplyAsync("", false,
                new EmbedBuilder {
                    Title = "Your queue consists of:",
                    Description = q
                }
                .WithColor(Color.DarkBlue)
                .Build());
        }

        [Command("all")]
        public async Task AllAsync() {
            string commands = "";
            foreach (var item in Services.CommandHandler._commandService?.Commands) {
                commands += item.Name + '\n';
            }

            await ReplyAsync(null, false,
                new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle("Music bot commands are:")
                .WithDescription(commands)
                .WithFooter(footer => footer.Text = "Pinn is the Best!")
                .Build());
        }

        public async Task<bool> IsConnectedBot() {
            if (!_lavaNode.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm not connected to a voice channel!");
                return false;
            }

            return true;
        }

        public async Task<bool> IsConnectedUserInTheSameVoiceChannel() {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel != _lavaNode.GetPlayer(Context.Guild).VoiceChannel) {
                await ReplyAsync("You need to be in the same voicechannel as me!");
                return false;
            }

            return true;
        }

        public async Task<bool> IsConnectedUser() {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null) {
                await ReplyAsync("You must be connected to a voice channel!");
                return false;
            }

            return true;
        }

        public async Task<Embed> CreateEmbed(LavaTrack track) {
            return new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithAuthor(track.Author)
                .WithTitle(track.Title)
                .WithUrl(track.Url)
                .WithThumbnailUrl(await track.FetchArtworkAsync())
                .WithFields(new EmbedFieldBuilder() {
                    Name = "Duration",
                    IsInline = true,
                    Value = track.Duration
                })
                .WithFields(new EmbedFieldBuilder() {
                    Name = "Requested By",
                    IsInline = true,
                    Value = (Context != null) ? Context.User.ToString() : "Context.User"
                })
                .WithFooter(footer => footer.Text = "Pinn is the Best!")
                .Build();
        }
    }
}

/*
    TO DO:
            - what the FUCK is the lyrics command !?
            - repeat feature is kind of shit implemented
            - np feature is 'ok' but the message is shown wrong
            - seek feature take string and split by ':' to make it friendly for seconds or sec and min or sec min and hours
            - any features left to add?
 */