﻿#region

using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Silk.Core.Database.Models;
using Silk.Core.Utilities;

#endregion

namespace Silk.Core.Commands.Server
{
    [Category(Categories.Server)]
    public class WhitelistInviteCommand : BaseCommandModule
    {
        [Command("Whitelist")]
        public async Task WhitelistInvite(CommandContext ctx, [RemainingText] params string[] links)
        {
            if (links.Any(l => l.StartsWith("remove")))
            {
                int removeIndex = links.ToList().IndexOf("remove") + 1;
                if (removeIndex > links.Length) await ctx.RespondAsync("You must supply a link to remove!");

                return;
            }

            GuildModel config = Core.Bot.Instance.SilkDBContext.Guilds.First(g => g.Id == ctx.Guild.Id);
            if (!config.WhitelistInvites)
            {
                await ctx.RespondAsync("This server doesn't whitelist invites!");
                return;
            }

            if (links.Length == 0)
            {
                await ctx.RespondAsync(
                    $"Whitelisted links: {string.Join(", ", config.WhiteListedLinks.Select(_ => $"`{_.Link}`"))}");
                return;
            }

            foreach (string invite in links)
            {
                var formattedInvite = string.Empty;
                formattedInvite = invite.Contains('/')
                    ? invite.Replace("discord.com/invite", "discord.gg/")
                    : "discord.gg/" + invite;
                config.WhiteListedLinks.Add(new WhiteListedLink {Link = formattedInvite});
            }

            await Core.Bot.Instance.SilkDBContext.SaveChangesAsync();
            await ctx.RespondAsync($"Whitelisted {links.Length} invites!");
        }
    }
}