﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Silk.Core.Database;
using Silk.Core.Database.Models;
using SilkBot.Extensions;

#endregion

namespace Silk.Core.Utilities
{
    public class BotEventHelper
    {
        private readonly IDbContextFactory<SilkDbContext> _dbFactory;
        private readonly ILogger<BotEventHelper> _logger;
        private readonly DiscordShardedClient _client;
        private readonly Stopwatch _time = new();
        private readonly object _obj = new();
        private bool _hasLoggedCompletion;
        private int _currentMemberCount;
        private int expectedMembers;
        private int cachedMembers;
        private int guildMembers;
        public BotEventHelper(DiscordShardedClient client, IDbContextFactory<SilkDbContext> dbFactory,
            ILogger<BotEventHelper> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _client = client;
        }

        public void CreateHandlers()
        {
            _client.ClientErrored += OnClientErrored;
            foreach (CommandsNextExtension c in _client.GetCommandsNextAsync().GetAwaiter().GetResult().Values)
                c.CommandErrored += CommandErrored;
        }

        private async Task CommandErrored(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            string message = e.Exception switch
            {
                CommandNotFoundException => $"Unkown command: {e.Command.Name}. Arguments: {e.Context.RawArgumentString}",
                InvalidOperationException {Message: "No matching subcommands were found, and this group is not executable."} => 
                    $"Unknown subcommand: {e.Command.Name} | Arguments: {e.Context.RawArgumentString}",
                {Message: "Could not find a suitable overload for the command."} => "SEND_HELP_MESSAGE",
                ChecksFailedException cf => cf.FailedChecks[0] switch
                    {
                        RequireFlagAttribute              f => $"You need {f.RequisiteUserFlag} for that!",
                        RequireNsfwAttribute                => "Channel must be maked as NSFW!",
                        RequireDirectMessageAttribute       => "This command is limited to direct messages!",
                        RequireGuildAttribute               => "This command is limited to servers!",
                        RequireUserPermissionsAttribute   p => $"You need to have {p.Permissions.Humanize(LetterCasing.Title)} to run that!", 
                        CooldownAttribute                cd => $"This command has a cooldown of ({cd.MaxUses} use / {cd.Reset.Humanize(minUnit: TimeUnit.Second)}. Come back in {cd.GetRemainingCooldown(e.Context).Humanize(3, minUnit: TimeUnit.Second)}",
                        _                                   => $"Something is limiting you from running this command.. This is the reason: `{cf.FailedChecks.Select(f => f.GetType().Name).JoinString("\n")}`"
                },

            _ => e.Exception.Message
            };

            if (message is "SEND_HELP_MESSGAGE") await SendHelpAsync(c.Client, e.Command.QualifiedName, e.Context);
            else if (message.Contains("Something is")) await e.Context.RespondAsync(message);
            else _logger.LogWarning(message);
        }
        
        private async Task SendHelpAsync(DiscordClient c, string commandName, CommandContext originalContext)
        {
            var cnext = c.GetCommandsNext();
            var cmd = cnext.RegisteredCommands["help"];
            var ctx = cnext.CreateContext(originalContext.Message, null, cmd, commandName);
            await cnext.ExecuteCommandAsync(ctx);
        }
        
        private async Task OnClientErrored(DiscordClient sender, ClientErrorEventArgs e)
        {
            if (e.Exception.Message.Contains("event"))
                _logger.LogWarning($"[{e.EventName}] Timed out.");
            else if (e.Exception.Message.ToLower().Contains("intents"))
                _logger.LogCritical("Intents aren't setup.");
        }


        private Task Cache(DiscordClient c, GuildCreateEventArgs e)
        {
            if (!_time.IsRunning)
            {
                _time.Start();
                _logger.LogTrace("Beginning Cache Run...");
            }

            _ = Task.Run(async () =>
            {
                guildMembers += e.Guild.MemberCount;
                
                using SilkDbContext db = _dbFactory.CreateDbContext();
                var sw = Stopwatch.StartNew();
                GuildModel? guild = db.Guilds.AsQueryable().Include(g => g.Users)
                                     .FirstOrDefault(g => g.Id == e.Guild.Id);
                sw.Stop();
                _logger.LogTrace($"Retrieved guild from database in {sw.ElapsedMilliseconds} ms.");

                if (guild is null)
                {
                    guild = new (){Id = e.Guild.Id, Prefix = Bot.DefaultCommandPrefix};
                    db.Guilds.Add(guild);
                }

                sw.Restart();
                CacheStaffMembers(guild, e.Guild.Members.Values);

                await db.SaveChangesAsync();

                sw.Stop();
                if (sw.ElapsedMilliseconds > 300)
                    _logger.LogWarning($"Databse query took longer than expected. (Expected <300ms, took {sw.ElapsedMilliseconds} ms)");
                _logger.LogDebug(
                    $"Shard [{c.ShardId + 1}/{c.ShardCount}] | Guild [{++_currentMemberCount}/{c.Guilds.Count}] | {sw.ElapsedMilliseconds}ms");
                if (_currentMemberCount == c.Guilds.Count && !_hasLoggedCompletion)
                {
                    _hasLoggedCompletion = true;
                    _time.Stop();
                    _logger.LogTrace("Cache run complete.");
                    if(expectedMembers < cachedMembers) _logger.LogWarning($"{expectedMembers} members flagged as staff from iterating over {guildMembers} members. [{cachedMembers}/{expectedMembers}] saved to db.");
                }
            });
            return Task.CompletedTask;
        }


        private void CacheStaffMembers(GuildModel guild, IEnumerable<DiscordMember> members)
        {
            IEnumerable<DiscordMember> staff = members.Where(m => m.HasPermission(Permissions.KickMembers & Permissions.ManageRoles) && !m.IsBot);
            
            foreach (DiscordMember member in staff)
            {
                var flags = UserFlag.Staff;
                if (member.HasPermission(Permissions.Administrator) || member.IsOwner) flags.Add(UserFlag.EscalatedStaff);

                UserModel? user = guild.Users.FirstOrDefault(u => u.Id == member.Id);
                if (user is not null) //If user exists
                {
                    if (!user.Flags.Has(UserFlag.Staff)) // Has flag
                        user.Flags.Add(UserFlag.Staff); // Add flag
                    if (member.HasPermission(Permissions.Administrator))
                        user.Flags.Add(UserFlag.EscalatedStaff);
                }
                else
                {
                    guild.Users.Add(new UserModel {Id = member.Id, Flags = flags});
                    
                }
            }
        }
    }
}