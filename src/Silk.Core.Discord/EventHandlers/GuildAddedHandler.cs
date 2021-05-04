﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Logging;
using Silk.Core.Data.MediatR.Users;
using Silk.Core.Data.Models;
using Silk.Core.Discord.EventHandlers.Notifications;
using Silk.Core.Discord.Types;
using Silk.Extensions;
using Silk.Shared.Constants;

namespace Silk.Core.Discord.EventHandlers
{
    //This relies on multiple events to update its state, so we can't implement INotificationHandler.
    // Correction. I'm stupid. You can implement multiple interfaces. Don't listen to the above comment. ~Velvet.//
    public class GuildAddedHandler //: INotificationHandler<GuildCreated>, INotificationHandler<GuildAvailable>, INotificationHandler<GuildDownloadCompleted>
    {
        private class ShardState
        {
            public bool Completed { get; set; }
            public int CachedGuilds { get; set; }
            public int CachedMembers { get; set; }
        }

        public static bool StartupCompleted { get; private set; }

        private readonly object _lock = new();
        private readonly ILogger<GuildAddedHandler> _logger;
        private readonly IMediator _mediator;
        private readonly Dictionary<int, ShardState> _shardStates = new();
        private const string OnGuildJoinThankYouMessage = "Hiya! My name is Silk! I hope to satisfy your entertainment and moderation needs. I respond to mentions and `s!` by default, but you can change the prefix by using the prefix command.\n" +
                                                          "Also! Development, hosting, infrastructure, etc. is expensive! Donations via [Patreon](https://patreon.com/VelvetThePanda) and [Ko-Fi](https://ko-fi.com/velvetthepanda) *greatly* aid in this endevour. <3";
        private int a = 0;
        private bool _logged;

        public GuildAddedHandler(ILogger<GuildAddedHandler> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
            IReadOnlyDictionary<int, DiscordClient> shards = Main.ShardClient.ShardClients;
            if (shards.Count is 0)
                throw new ArgumentOutOfRangeException(nameof(DiscordClient.ShardCount), "Shards must be greater than 0");

            foreach ((int key, _) in shards)
                _shardStates.Add(key, new());
        }

        /// <summary>
        ///     Caches and logs members when GUILD_AVAILABLE is fired via the gateway.
        /// </summary>
        public async Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            //await Task.Yield();
            await Task.Delay(950);
            //await _mediator.Send(new GetOrCreateGuildRequest(eventArgs.Guild.Id, Main.DefaultCommandPrefix));
            int cachedMembers = await CacheGuildMembers(eventArgs.Guild.Members.Values);

            lock (_lock)
            {
                Main.ChangeState(BotState.Caching);
                ShardState state = _shardStates[client.ShardId];
                state.CachedMembers += cachedMembers;
                ++state.CachedGuilds;
                _shardStates[client.ShardId] = state;
                if (!StartupCompleted)
                {
                    string message;
                    if (cachedMembers is 0)
                    {
                        message = "Cached Guild! Shard [{shard}/{shards}] → Guild [{currentGuild}/{guilds}] → Staff [No new staff!]";
                        _logger.LogDebug(message, client.ShardId + 1,
                            Main.ShardClient.ShardClients.Count,
                            state.CachedGuilds, client.Guilds.Count);
                    }
                    else
                    {
                        message = "Cached Guild! Shard [{shard}/{shards}] → Guild [{currentGuild}/{guilds}] → Staff [{members}/{allMembers}]";
                        _logger.LogDebug(message, client.ShardId + 1,
                            Main.ShardClient.ShardClients.Count,
                            state.CachedGuilds, client.Guilds.Count,
                            cachedMembers, eventArgs.Guild.Members.Count);
                    }
                }
            }
        }

        public async Task OnGuildDownloadComplete(DiscordClient c, GuildDownloadCompletedEventArgs e)
        {
            ShardState state = _shardStates[c.ShardId];
            state.Completed = true;
            _shardStates[c.ShardId] = state;
            StartupCompleted = _shardStates.Values.All(s => s.Completed);
            if (StartupCompleted && !_logged)
            {
                _logger.LogDebug("All shard(s) cache runs complete!");
                _logged = true;
                Main.ChangeState(BotState.Ready);
            }
        }


        // Used in conjunction with OnGuildJoin() //
        public async Task SendThankYouMessage(DiscordClient c, GuildCreateEventArgs e)
        {
            var allChannels = (await e.Guild.GetChannelsAsync()).OrderBy(channel => channel.Position);
            DiscordMember bot = e.Guild.CurrentMember;
            DiscordChannel? availableChannel =
                allChannels.Where(c => c.Type is ChannelType.Text)
                    .FirstOrDefault(c => c.PermissionsFor(bot).HasPermission(Permissions.SendMessages | Permissions.EmbedLinks));
            if (availableChannel is null) return;


            var builder = new DiscordEmbedBuilder()
                .WithTitle("Thank you for adding me!")
                .WithColor(new("94f8ff"))
                .WithDescription(OnGuildJoinThankYouMessage)
                .WithThumbnail("https://files.velvetthepanda.dev/silk.png")
                .WithFooter("Silk! | Made by Velvet & Contributors w/ <3");
            await availableChannel.SendMessageAsync(builder);
        }

        private async Task<int> CacheGuildMembers(IEnumerable<DiscordMember> members)
        {
            int staffCount = 0;
            IEnumerable<DiscordMember> staff = members.Where(m => !m.IsBot);

            foreach (var member in staff)
            {
                UserFlag flag = member.HasPermission(Permissions.Administrator) || member.IsOwner ? UserFlag.EscalatedStaff : UserFlag.Staff;

                User? user = await _mediator.Send(new GetUserRequest(member.Guild.Id, member.Id));
                if (user is not null)
                {
                    if (member.HasPermission(Permissions.Administrator) || member.IsOwner && !user.Flags.Has(UserFlag.EscalatedStaff))
                    {
                        user.Flags.Add(UserFlag.EscalatedStaff);
                    }
                    else if (member.HasPermission(FlagConstants.CacheFlag))
                    {
                        user.Flags.Add(UserFlag.Staff);
                    }
                    else
                    {
                        if (user.Flags.Has(UserFlag.Staff))
                        {
                            UserFlag f = user.Flags.Has(UserFlag.EscalatedStaff) ? UserFlag.EscalatedStaff : UserFlag.Staff;
                            user.Flags.Remove(f);
                        }
                    }
                    await _mediator.Send(new UpdateUserRequest(member.Guild.Id, member.Id, user.Flags));
                }
                else if (member.HasPermission(FlagConstants.CacheFlag) || member.IsAdministrator() || member.IsOwner)
                {
                    await _mediator.Send(new AddUserRequest(member.Guild.Id, member.Id, flag));
                    staffCount++;
                }
            }
            return Math.Max(staffCount, 0);
        }
        public async Task Handle(GuildAvailable notification, CancellationToken cancellationToken) => await OnGuildAvailable(notification.Client, notification.Args);
        public Task Handle(GuildDownloadCompleted notification, CancellationToken cancellationToken) => OnGuildDownloadComplete(notification.Client, notification.Args);
        public Task Handle(GuildCreated notification, CancellationToken cancellationToken) => Task.WhenAll(SendThankYouMessage(notification.Client, notification.Args), OnGuildAvailable(notification.Client, notification.Args));
    }
}