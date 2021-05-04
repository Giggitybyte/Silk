﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silk.Core.Discord.EventHandlers;
using Silk.Core.Discord.EventHandlers.MemberAdded;
using Silk.Core.Discord.EventHandlers.Notifications;
using Silk.Core.Discord.Types;
using Silk.Core.Discord.Utilities;
using Silk.Core.Discord.Utilities.Bot;
using Silk.Core.Discord.Utilities.HelpFormatter;
using Silk.Extensions;

namespace Silk.Core.Discord
{
    public class Main : IHostedService
    {
        public static BotState State { get; private set; } = BotState.Starting;
        //public static DiscordSlashClient SlashClient { get; } // Soon™ //
        public static DiscordShardedClient ShardClient { get; private set; }
        public static string DefaultCommandPrefix { get; } = "s!";

        private static ILogger<Main> _logger;
        private readonly IServiceProvider _provider;

        public Main(DiscordShardedClient shardClient, ILogger<Main> logger, IServiceProvider provider)
        {
            _logger = logger;
            _provider = provider;
            ShardClient = shardClient;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void ChangeState(BotState state)
        {
            if (State != state)
            {
                _logger.LogDebug("State changed from {State} to {NewState}!", State, state);
                State = state;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service");
            await InitializeClientAsync();
            await InitializeCommandsNextAsync();
            _logger.LogDebug("Connecting to Discord gateway");
            await ShardClient.StartAsync();
            _logger.LogInformation("Connected to Discord gateway as {Username}#{Discriminator}", ShardClient.CurrentUser.Username, ShardClient.CurrentUser.Discriminator);
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service");
            _logger.LogDebug("Disconnecting from Discord gateway");
            await ShardClient.StopAsync();
            _logger.LogInformation("Disconnected from Discord gateway");
        }

        private async Task InitializeClientAsync()
        {
            _logger.LogDebug("Initializing client");

            await ShardClient.UseCommandsNextAsync(DiscordConfigurations.CommandsNext);
            await ShardClient.UseInteractivityAsync(DiscordConfigurations.Interactivity);

            SubscribeToEvents();
        }
        private void SubscribeToEvents()
        {
            //Client.MessageCreated += _services.Get<AutoModInviteHandler>().MessageAddInvites; // I'll fix AutoMod eventually™ ~Velvet, May 3rd, 2021. //
            var services = _provider;
            var mediator = services.Get<IMediator>()!;

            // Direct Dispatch //
            ShardClient.MessageDeleted += services.Get<MessageRemovedHandler>()!.MessageRemoved;
            ShardClient.GuildMemberAdded += services.Get<MemberAddedHandler>()!.OnMemberAdded;
            ShardClient.GuildMemberUpdated += services.Get<RoleAddedHandler>()!.CheckStaffRole;

            // MediatR Dispatch //

            ShardClient.GuildDownloadCompleted += async (cl, __) =>
                cl.MessageCreated += async (c, e) => { _ = mediator.Publish(new MessageCreated(c, e.Message!)); };

            ShardClient.GuildCreated += async (c, e) => { _ = mediator.Publish(new GuildCreated(c, e)); };
            ShardClient.GuildAvailable += async (c, e) =>
            {
                var sw = Stopwatch.StartNew();
                await Task.WhenAll(mediator.Publish(new GuildAvailable(c, e)), Task.Run(() =>
                {
                    while (true) _logger.LogTrace("Execution time: {ExTime}", sw.ElapsedMilliseconds);
                }));
            };
            //ShardClient.GuildAvailable += services.Get<GuildAddedHandler>()!.OnGuildAvailable;
            //ShardClient.GuildAvailable += async (_, _) => await Task.Delay(980);

            ShardClient.GuildDownloadCompleted += async (c, e) => { _ = mediator.Publish(new GuildDownloadCompleted(c, e)); };

            ShardClient.MessageUpdated += async (c, e) => { _ = mediator.Publish(new MessageEdited(c, e)); };
        }

        private async Task InitializeCommandsNextAsync()
        {
            _logger.LogDebug("Registering commands");

            var t = Stopwatch.StartNew();
            var asm = Assembly.GetExecutingAssembly();
            var cnext = await ShardClient.GetCommandsNextAsync();

            foreach (var cnextExt in cnext.Values)
            {
                cnextExt.RegisterCommands(asm);
                cnextExt.SetHelpFormatter<HelpFormatter>();
                cnextExt.RegisterConverter(new MemberConverter());
            }

            t.Stop();
            var registeredCommands = cnext.Values.Sum(r => r.RegisteredCommands.Count);

            _logger.LogDebug("Registered {Commands} commands for {Shards} shards in {Time} ms", registeredCommands, ShardClient.ShardClients.Count, t.ElapsedMilliseconds);
        }
    }
}