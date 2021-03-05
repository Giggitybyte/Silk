﻿using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Silk.Core.Utilities;
using Silk.Core.Utilities.HelpFormatter;
using Silk.Data.Models;
using Silk.Extensions;

namespace Silk.Core.Commands.Moderation
{
    [Category(Categories.Mod)]
    public class UnbanCommand : BaseCommandModule
    {
        [Command("unban")]
        [RequireUserPermissions(Permissions.BanMembers)]
        [Description("Unban a member from the Guild")]
        public async Task UnBan(CommandContext ctx, DiscordUser user, [RemainingText] string reason = "No reason given.")
        {
            if ((await ctx.Guild.GetBansAsync()).Select(b => b.User.Id).Contains(user.Id))
            {
                await user.UnbanAsync(ctx.Guild, reason);
                DiscordEmbedBuilder embed =
                    new DiscordEmbedBuilder(EmbedHelper.CreateEmbed(ctx, "",
                        $"Unbanned {user.Username}#{user.Discriminator} `({user.Id})`! ")).AddField("Reason:", reason);

                //TODO: Refactor this to use IInfractionService

                // var infraction =
                //     (TimedInfraction) _eventService.Events.FirstOrDefault(e => ((TimedInfraction) e).Id == user.Id);
                // if (infraction is not null) _eventService.Events.TryRemove(infraction);

                await ctx.RespondAsync(embed);
            }
            else
            {
                DiscordEmbedBuilder embed =
                    new DiscordEmbedBuilder(EmbedHelper.CreateEmbed(ctx, "", $"{user.Mention} is not banned!"))
                        .WithColor(new("#d11515"));

                await ctx.RespondAsync(embed);
            }
        }
    }
}