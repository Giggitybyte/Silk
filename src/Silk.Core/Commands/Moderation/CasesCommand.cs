﻿using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using MediatR;
using Silk.Core.Utilities;
using Silk.Data.MediatR;
using Silk.Data.Models;
using Silk.Extensions;

namespace Silk.Core.Commands.Moderation
{
    [Experimental]
    // Read the VC chat; give me terrible ideas to implement and @ me
    public class CasesCommand : BaseCommandModule
    {
        private readonly IMediator _mediator;
        public CasesCommand(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Command]
        [RequireGuild]
        [RequireFlag(UserFlag.Staff)]
        public async Task Cases(CommandContext ctx, DiscordUser user)
        {
            var mBuilder = new DiscordMessageBuilder().WithReply(ctx.Message.Id);
            var eBuilder = new DiscordEmbedBuilder();

            Guild guild = await _mediator.Send(new GuildRequest.Get(ctx.Guild.Id));
            bool userExists = await _mediator.Send(new UserRequest.Get(ctx.Guild.Id, user.Id)) is not null;
            
            if (!userExists || guild.Infractions.Count(i => i.UserId == user.Id) is 0)
            {
                mBuilder.WithContent("User has no cases!");
                await ctx.RespondAsync(mBuilder);
            }
            else
            {
                var sb = new StringBuilder();
                for (int i = 0; i < guild.Infractions.Count; i++)
                {
                    var currentInfraction = guild.Infractions[i];
                    if (currentInfraction.UserId == user.Id)
                    {
                        sb.AppendLine($"Case {i + 1}: {currentInfraction.InfractionType.Humanize(LetterCasing.Title)} by <@{currentInfraction.Enforcer}>, " +
                                      $"Reason:\n{currentInfraction.Reason[..(currentInfraction.Reason.Length > 100 ? 100 : ^0)]}");
                    }
                }
                
                eBuilder
                    .WithColor(DiscordColor.Gold)
                    .WithTitle($"Cases for {user.Id}")
                    .WithDescription(sb.ToString());
                mBuilder.WithEmbed(eBuilder);
                
                await ctx.RespondAsync(mBuilder);
            }
        }
    }
}