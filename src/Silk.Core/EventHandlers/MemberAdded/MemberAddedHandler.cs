﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Silk.Core.Data.Models;
using Silk.Core.Services;

namespace Silk.Core.EventHandlers.MemberAdded
{
    public class MemberAddedHandler
    {
        private readonly ConfigService _configService;

        private readonly Timer _timer = new(500);
        public MemberAddedHandler(ConfigService configService, ILogger<MemberAddedHandler> logger)
        {
            _configService = configService;
            _timer.AutoReset = true;
            _timer.Elapsed += async (_, _) => _ = OnTick();
            _timer.Start();
        }
        public List<DiscordMember> MemberQueue { get; private set; } = new();

        public async Task OnMemberAdded(DiscordClient c, GuildMemberAddEventArgs e)
        {
            GuildConfig config = await _configService.GetConfigAsync(e.Guild.Id);
            // This should be done in a seperate service //
            if (config.LogMemberJoins && config.LoggingChannel is not 0)
                await e.Guild.GetChannel(config.LoggingChannel).SendMessageAsync(GetJoinEmbed(e));

            bool screenMembers = e.Guild.Features.Contains("MEMBER_VERIFICATION_GATE_ENABLED") && config.GreetOnScreeningComplete;
            bool verifyMembers = config.GreetOnVerificationRole && config.VerificationRole is not 0;

            if (screenMembers || verifyMembers)
                MemberQueue.Add(e.Member);
            else await GreetMemberAsync(e.Member, config);
        }

        private static async Task GreetMemberAsync(DiscordMember member, GuildConfig config)
        {
            bool shouldGreet = config.GreetMembers;
            bool hasValidGreetingChannel = config.GreetingChannel is not 0;
            bool hasValidGreetingMessage = !string.IsNullOrWhiteSpace(config.GreetingText);
            if (shouldGreet && hasValidGreetingChannel && hasValidGreetingMessage)
            {
                DiscordChannel channel = member.Guild.GetChannel(config.GreetingChannel);
                string formattedMessage = config.GreetingText
                    .Replace("{u}", member.Username)
                    .Replace("{s}", member.Guild.Name)
                    .Replace("{@u}", member.Mention)
                    .Replace("\\n", "\n");

                await channel.SendMessageAsync(formattedMessage);
            }
        }

        private async Task OnTick()
        {
            if (MemberQueue.Count is 0) return;
            var verifiedMembers = new List<DiscordMember>();
            foreach (DiscordMember member in MemberQueue)
            {
                GuildConfig config = await _configService.GetConfigAsync(member.Guild.Id);

                if (config.GreetOnScreeningComplete && member.IsPending is true) return;
                if (config.GreetOnVerificationRole && member.Roles.All(r => r.Id != config.VerificationRole)) return;
                verifiedMembers.Add(member);
                await GreetMemberAsync(member, config);
            }

            if (verifiedMembers.Any())
            {
                MemberQueue = MemberQueue.Except(verifiedMembers).ToList();
            }
        }

        private static DiscordEmbedBuilder GetJoinEmbed(GuildMemberAddEventArgs e)
        {
            return new DiscordEmbedBuilder()
                .WithTitle("User joined:")
                .WithDescription($"User: {e.Member.Mention}")
                .AddField("User ID:", e.Member.Id.ToString(), true)
                .WithThumbnail(e.Member.AvatarUrl)
                .WithColor(DiscordColor.Green);
        }
    }
}