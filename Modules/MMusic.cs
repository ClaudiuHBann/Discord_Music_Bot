﻿using Discord.Commands;
using Discord;

using Victoria;
using Victoria.Node;

using System.Text.RegularExpressions;
using System.Text;
using Victoria.Player;
using Victoria.Node.EventArgs;

namespace DiscordMusicBot.Modules {
    public class MMusic : ModuleBase<SocketCommandContext> {
        private readonly LavaNode _lavaNode;
        private static bool repeat = false;

        public MMusic(LavaNode lavaNode) {
            _lavaNode = lavaNode;
            _lavaNode.OnTrackEnd += OnTrackEndedAsync;
        }

        private async Task OnTrackEndedAsync(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
            if (arg.Player.PlayerState == PlayerState.Stopped) {
                if (repeat) {
                    await arg.Player.PlayAsync(arg.Track);
                } else {
                    if (arg.Player.Vueue != null && arg.Player.Vueue.Count > 0) {
                        await arg.Player.SkipAsync();
                        await arg.Player.TextChannel.SendMessageAsync("Now Playing...", false, await CreateEmbed(arg.Player.Track));
                    }
                }
            }
        }

        [Command("join")]
        [Summary("joins your voice channel")]
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

        [Command("play"), Alias("p")]
        [Summary("plays your track request (will join your voice channel too if necessary)")]
        public async Task PlayAsync([Remainder] string query) {
            if (string.IsNullOrWhiteSpace(query)) {
                await ReplyAsync("Please provide search terms.");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild)) {
                await JoinAsync();
            }

            var searchResponse = await _lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.YouTube, query);
            if (searchResponse.Status == Victoria.Responses.Search.SearchStatus.LoadFailed ||
                searchResponse.Status == Victoria.Responses.Search.SearchStatus.NoMatches) {

                searchResponse = await _lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.YouTubeMusic, query);
                if (searchResponse.Status == Victoria.Responses.Search.SearchStatus.LoadFailed ||
                    searchResponse.Status == Victoria.Responses.Search.SearchStatus.NoMatches) {
                    await ReplyAsync($"I wasn't able to find anything for '{query}'.");
                    return;
                }
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            var track = searchResponse.Tracks.ToArray()[0];
            if (player.PlayerState == PlayerState.Playing ||
                player.PlayerState == PlayerState.Paused) {
                player.Vueue.Enqueue(track);
                await ReplyAsync($"Enqueued: {track.Title}");
            } else {
                await player.PlayAsync(track);
                await ReplyAsync(null, false, await CreateEmbed(track));
            }
        }

        [Command("skip")]
        [Summary("stops the playing track and plays next track from queue if exists")]
        public async Task SkipAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (player.Vueue.Count == 0) {
                await player.StopAsync();
                return;
            }

            await player.SkipAsync();
            await ReplyAsync(null, false, await CreateEmbed(player.Track));
        }

        [Command("pause")]
        [Summary("pauses the playing track.")]
        public async Task PauseAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (player.PlayerState == PlayerState.Paused ||
                player.PlayerState == PlayerState.Stopped) {
                await ReplyAsync("The music is already paused!");
                return;
            }

            await player.PauseAsync();
            await ReplyAsync("Paused the music!");
        }

        [Command("resume")]
        [Summary("resumes the paused track")]
        public async Task ResumeAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (player.PlayerState == PlayerState.Playing) {
                await ReplyAsync("The music is already playing!");
                return;
            }

            await player.ResumeAsync();
            await ReplyAsync("Resumed the music!");
        }

        [Command("stop")]
        [Summary("stops the playing track")]
        public async Task StopAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (player.PlayerState != PlayerState.Playing) {
                await ReplyAsync("No music playing!");
                return;
            }

            await player.StopAsync();
            await ReplyAsync("Stopped the music!");
        }

        [Command("leave")]
        [Summary("leaves the voice channel")]
        public async Task LeaveAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            var voiceState = Context.User as IVoiceState;
            await _lavaNode.LeaveAsync(voiceState?.VoiceChannel);
        }

        [Command("clear"), Alias("clr")]
        [Summary("clears the queue or a part of it")]
        public async Task ClearAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            player.Vueue.Clear();
        }

        [Command("jump")]
        [Summary("jumps over a range of tracks")]
        public async Task JumpAsync(int amount) {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (amount <= player.Vueue.Count) {
                player.Vueue.RemoveRange(0, amount - 1);

                if (player.Vueue.Count > 0) {
                    await player.SkipAsync();
                }
            } else {
                await ReplyAsync("The command's argument is wrong!");
            }
        }

        [Command("remove"), Alias("delete")]
        [Summary("not implemented yet")]
        public async Task RemoveAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }
        }

        [Command("repeat")]
        [Summary("repeats the playing track forever muahaha :)")]
        public async Task RepeatAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (repeat = !repeat) {
                await ReplyAsync("I am going to repeat...", false, await CreateEmbed(player.Track));
            } else {
                await ReplyAsync("I have stopped repeating...", false, await CreateEmbed(player.Track));
            }
        }

        [Command("shuffle")]
        [Summary("shuffles the queue")]
        public async Task ShuffleAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            player.Vueue.Shuffle();
        }

        [Command("seek")]
        [Summary("seeks the playing track")]
        public async Task SeekAsync([Remainder] string timeSpan) {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (!player.Track.CanSeek) {
                await ReplyAsync("The track is unseekable!");
                return;
            }

            if (Regex.IsMatch(timeSpan, "^[-:0-9]+$")) {
                string[] tsSplit = timeSpan.Split(':');
                TimeSpan ts;
                if (tsSplit.Length == 1) {
                    ts = new TimeSpan(0, 0, int.Parse(tsSplit[0]));
                } else if (tsSplit.Length == 2) {
                    ts = new TimeSpan(0, int.Parse(tsSplit[0]), int.Parse(tsSplit[1]));
                } else {
                    ts = new TimeSpan(int.Parse(tsSplit[0]), int.Parse(tsSplit[1]), int.Parse(tsSplit[2]));
                }

                if (ts > player.Track.Duration) {
                    await ReplyAsync("The command's argument is wrong!");
                    return;
                }

                await player.SeekAsync(ts);
            } else {
                await ReplyAsync("The command's argument is wrong!");
            }
        }

        [Command("nowplaying"), Alias("np")]
        [Summary("shows information about the playing track")]
        public async Task NPAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            var track = player.Track;

            TimeSpan position = track.Position, duration = track.Duration;
            int posInBar = (int)((double)position.Ticks / duration.Ticks * 10.0);

            string radioButton = "🔘";
            string barChar = "▬";
            string bar = "";

            for (int i = 0; i < 10; i++) {
                bar += (i == posInBar) ? radioButton : barChar;
            }

            await ReplyAsync(null, false,
                new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithAuthor(track.Author)
                .WithTitle(track.Title)
                .WithUrl(track.Url)
                .WithThumbnailUrl(await track.FetchArtworkAsync())
                .WithDescription($"{position.ToString(@"hh\:mm\:ss")} {bar} {duration}")
                .WithFields(new EmbedFieldBuilder() {
                    Name = "Requested By",
                    IsInline = true,
                    Value = "Context.User"
                })
                .WithFooter(footer => footer.Text = "Pinn is the Best!")
                .Build());
        }

        [Command("search")]
        [Summary("searches for your track request and shows the best matches")]
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
        [Summary("shows the lyrics for the playing track")]
        public async Task LyricsAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (player.PlayerState != PlayerState.Playing) {
                await ReplyAsync("I'm not playing any tracks.");
                return;
            }

            var track = player.Track;
            var lyrics = await track.FetchLyricsFromOvhAsync();
            if (string.IsNullOrWhiteSpace(lyrics)) {
                await ReplyAsync($"No lyrics found for {track.Title}");
                return;
            }

            var splitLyrics = lyrics.Split('\n');
            var stringBuilder = new StringBuilder();
            foreach (var line in splitLyrics) {
                if (Enumerable.Range(1900, 2000).Contains(stringBuilder.Length)) {
                    await ReplyAsync($"```{stringBuilder}```");
                    stringBuilder.Clear();
                } else {
                    stringBuilder.AppendLine(line);
                }
            }

            await ReplyAsync(null, false, new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle(track.Title)
                .WithDescription(stringBuilder.ToString())
                .WithThumbnailUrl(await track.FetchArtworkAsync())
                .WithFooter(footer => footer.Text = "Pinn is the Best!")
                .Build());
        }

        [Command("queue"), Alias("q")]
        [Summary("shows the queue")]
        public async Task QueueAsync() {
            if (!await IsConnectedUser() || !await IsConnectedBot() || !await IsConnectedUserInTheSameVoiceChannel()) {
                return;
            }

            string q = "";
            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            var queue = player.Vueue.ToArray();
            for (int i = 1; i <= queue.Length; i++) {
                q += i + ". " + queue[i - 1].Title + "\n";
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
        [Summary("shows all the commands (aliases) - summaries")]
        public async Task AllAsync() {
            string all = "";
            CommandInfo[]? array = Services.CommandHandler._commandService?.Commands.ToArray();
            for (int i1 = 0; i1 < array?.Length; i1++) {
                CommandInfo? command = array[i1];
                string aliases = "";
                for (int i = 1; i < command.Aliases.Count; i++) {
                    aliases += command.Aliases[i];

                    if (i != command.Aliases.Count - 1) {
                        aliases += ", ";
                    }
                }

                if (aliases == "") {
                    all += $"**{command.Name}** - *{command.Summary}*\n";
                } else {
                    all += $"**{command.Name}** (**{aliases}**) - *{command.Summary}*\n";
                }
            }

            await ReplyAsync(null, false,
                new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle("Music commands are:")
                .WithDescription(all)
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
            _lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player);
            if (voiceState?.VoiceChannel != player.VoiceChannel) {
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
            - any features left to add?
            - on command succesfully sometimes nothing is returned to user visually
            - when skip or play sometimes no track visualization appears to user
            - add delete/remove command (deletes/removes tracks from queue)
            - search for tracks in cloud too not just youtube
            - leave when no music is playing or when no1 is in voice channel
 */
