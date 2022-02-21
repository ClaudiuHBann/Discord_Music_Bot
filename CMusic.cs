using Discord;
using Discord.Commands;
using Victoria;
using Victoria.EventArgs;

namespace DiscordMusicBot {
    public class CMusic : ModuleBase<SocketCommandContext> {
        private readonly LavaNode _lavaNode;
        private static bool repeat = false;

        public CMusic(LavaNode lavaNode) {
            _lavaNode = lavaNode;

            _lavaNode.OnTrackEnded += OnTrackEndedAsync;

            Program.SetLavaNode(lavaNode);
            Console.WriteLine("lava node created");
        }

        private async Task OnTrackEndedAsync(TrackEndedEventArgs arg) {
            if (arg.Player.PlayerState == Victoria.Enums.PlayerState.Stopped) {
                if (repeat) {
                    await arg.Player.PlayAsync(arg.Track);
                } else {
                    if (arg.Player.Queue.Count > 0) {
                        await arg.Player.SkipAsync();
                        await arg.Player.TextChannel.SendMessageAsync("Now Playing...", false, await CreateEmbed(arg.Player.Track));
                    }
                }
            }
        }

        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinAsync() {
            if (_lavaNode.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null) {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            try {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
            } catch (Exception exception) {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("play", RunMode = RunMode.Async)]
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

            if (player.PlayerState == Victoria.Enums.PlayerState.Playing ||
                player.PlayerState == Victoria.Enums.PlayerState.Paused) {
                var track = searchResponse.Tracks.ToArray()[0];
                player.Queue.Enqueue(track);

                await ReplyAsync($"Enqueued: {track.Title}");
            } else {
                var track = searchResponse.Tracks.ToArray()[0];

                await player.PlayAsync(track);
                await ReplyAsync(null, false, await CreateEmbed(track));
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        public async Task SkipAsync() {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null) {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm not connected to a voice channel!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel) {
                await ReplyAsync("You need to be in the same voicechannel as me!");
                return;
            }

            if (player.Queue.Count == 0) {
                await player.StopAsync();
                return;
            }

            await player.SkipAsync();
            await ReplyAsync(null, false, await CreateEmbed(player.Track));
        }

        [Command("pause", RunMode = RunMode.Async)]
        public async Task PauseAsync() {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null) {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm not connected to a voice channel!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel) {
                await ReplyAsync("You need to be in the same voicechannel as me!");
                return;
            }

            if (player.PlayerState == Victoria.Enums.PlayerState.Paused ||
                player.PlayerState == Victoria.Enums.PlayerState.Stopped) {
                await ReplyAsync("The music is already paused!");
                return;
            }

            await player.PauseAsync();
            await ReplyAsync("Paused the music!");
        }

        [Command("resume", RunMode = RunMode.Async)]
        public async Task ResumeAsync() {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null) {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm not connected to a voice channel!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel) {
                await ReplyAsync("You need to be in the same voicechannel as me!");
                return;
            }

            if (player.PlayerState == Victoria.Enums.PlayerState.Playing) {
                await ReplyAsync("The music is already playing!");
                return;
            }

            await player.ResumeAsync();
            await ReplyAsync("Resumed the music!");
        }






        [Command("stop", RunMode = RunMode.Async)]
        public async Task StopAsync() {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null) {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm not connected to a voice channel!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel) {
                await ReplyAsync("You need to be in the same voicechannel as me!");
                return;
            }

            if (player.PlayerState != Victoria.Enums.PlayerState.Playing) {
                await ReplyAsync("No music playing!");
                return;
            }

            await player.StopAsync();
            await ReplyAsync("Stopped the music!");
        }

        [Command("leave", RunMode = RunMode.Async)]
        public async Task LeaveAsync() {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null) {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild)) {
                await ReplyAsync("I'm not connected to a voice channel!");
                return;
            }

            try {
                await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
            } catch (Exception exception) {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("clear", RunMode = RunMode.Async)]
        [Alias("clr")]
        public async Task ClearAsync() {
            _lavaNode.GetPlayer(Context.Guild).Queue.Clear();
        }

        [Command("repeat", RunMode = RunMode.Async)]
        public async Task RepeatAsync() {
            repeat = !repeat;

            if (repeat) {
                await ReplyAsync("I am going to repeat...", false, await CreateEmbed(_lavaNode.GetPlayer(Context.Guild).Track));
            } else {
                await ReplyAsync("I have stopped repeating...", false, await CreateEmbed(_lavaNode.GetPlayer(Context.Guild).Track));
            }
        }

        [Command("shuffle", RunMode = RunMode.Async)]
        public async Task ShuffleAsync() {
            _lavaNode.GetPlayer(Context.Guild).Queue.Shuffle();
        }

        [Command("seek", RunMode = RunMode.Async)]
        public async Task SeekAsync(int hours, int minutes, int seconds) {
            if (_lavaNode.GetPlayer(Context.Guild).Track.CanSeek) {
                await _lavaNode.GetPlayer(Context.Guild).SeekAsync(new TimeSpan(hours, minutes, seconds));
            }
        }

        [Command("np", RunMode = RunMode.Async)]
        public async Task NPAsync() {
            await ReplyAsync(_lavaNode.GetPlayer(Context.Guild).Track.Position.ToString(), false, await CreateEmbed(_lavaNode.GetPlayer(Context.Guild).Track));
        }

        [Command("search", RunMode = RunMode.Async)]
        public async Task SearchAsync([Remainder] string query) {
            await _lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.YouTube, query).ContinueWith(async (a) => {
                string tracks = "";

                foreach (var item in a.Result.Tracks) {
                    tracks += item.Title + '\n';
                }

                await ReplyAsync("Tracks found on youtube:", false, new EmbedBuilder()
                    .WithColor(Color.DarkBlue)
                    .WithDescription(tracks)
                    .WithFooter(footer => footer.Text = "Smecherie tata! :))")
                    .Build());
            });
        }

        [Command("lyrics", RunMode = RunMode.Async)]
        public async Task LyricsAsync() {
            var track = _lavaNode.GetPlayer(Context.Guild).Track;

            Console.WriteLine("0");
            string lyricsFromGenius = track.FetchLyricsFromGeniusAsync().GetAwaiter().GetResult();
            if (lyricsFromGenius != null && lyricsFromGenius != "") {
                Console.WriteLine("1");
                await ReplyAsync(null, false, new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle(track.Title)
                .WithDescription(lyricsFromGenius)
                .WithThumbnailUrl(await track.FetchArtworkAsync())
                .WithFooter(footer => footer.Text = "Smecherie tata! :))")
                .Build());
            } else {
                Console.WriteLine("2");
                string lyricsFromOvh = track.FetchLyricsFromOvhAsync().GetAwaiter().GetResult();
                if (lyricsFromOvh != null && lyricsFromOvh != "") {
                    await ReplyAsync(null, false, new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle(track.Title)
                .WithDescription(lyricsFromOvh)
                .WithThumbnailUrl(await track.FetchArtworkAsync())
                .WithFooter(footer => footer.Text = "Smecherie tata! :))")
                .Build());
                } else {
                    await ReplyAsync("No lyrics found for " + _lavaNode.GetPlayer(Context.Guild).Track.Title);
                }
            }

            Console.WriteLine("-1");
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        public async Task QueueAsync() {
            string q = "";

            var queue = _lavaNode.GetPlayer(Context.Guild).Queue.ToArray();
            int i = 1;
            foreach (var item in queue) {
                q += i++.ToString() + ". " + item.Title + "\n";
            }

            var embed = new EmbedBuilder {
                Title = "Your queue consists of:",
                Description = q
            };
            embed.WithColor(Color.DarkBlue);

            await ReplyAsync("", false, embed.Build());
        }

        [Command("jump", RunMode = RunMode.Async)]
        public async Task JumpAsync(int numberOfTracks) {
            if (numberOfTracks > _lavaNode.GetPlayer(Context.Guild).Queue.Count) {
                await ReplyAsync($"There are not {numberOfTracks} or more tracks in the queue.");
            } else {
                _lavaNode.GetPlayer(Context.Guild).Queue.RemoveRange(0, numberOfTracks);
            }
        }

        [Command("all", RunMode = RunMode.Async)]
        public async Task AllAsync() {
            string commands = "";

            foreach (var item in Program.GetCommandService().Commands) {
                commands += item.Name + '\n';
            }

            await ReplyAsync(null, false, new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle("Music bot commands are:")
                .WithDescription(commands)
                .WithFooter(footer => footer.Text = "Smecherie tata! :))")
                .Build());
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
                    Value = Context.User.ToString() ?? "Context.User"
                })
                .WithFooter(footer => footer.Text = "Smecherie tata! :))")
                .Build();
        }
    }
}
