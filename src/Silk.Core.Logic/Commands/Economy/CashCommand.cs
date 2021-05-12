﻿using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Silk.Core.Data;
using Silk.Core.Data.Models;
using Silk.Core.Discord.Utilities;
using Silk.Core.Discord.Utilities.HelpFormatter;

namespace Silk.Core.Logic.Commands.Economy
{
    [Category(Categories.Economy)]
    public class CashCommand : BaseCommandModule
    {
        private readonly IDbContextFactory<GuildContext> _dbFactory;

        public CashCommand(IDbContextFactory<GuildContext> dbContextFactory)
        {
            _dbFactory = dbContextFactory;
        }

        [Command("cash")]
        [Aliases("money", "bal", "balance", "bank")]
        [Description("See how much cash you have in your economy account :)")]
        public async Task Cash(CommandContext ctx)
        {
            GuildContext db = _dbFactory.CreateDbContext();
            GlobalUser? account = db.GlobalUsers.FirstOrDefault(u => u.Id == ctx.User.Id);
            if (account is null)
            {
                await ctx.RespondAsync("Seems you don't have an account. " +
                                       $"Use `{ctx.Prefix}daily` and I'll set one up for you *:)*");
                return;
            }

            DiscordEmbedBuilder eb = EmbedHelper
                .CreateEmbed(ctx, "Account balance:", $"You have {account.Cash} dollars!")
                .WithAuthor(ctx.User.Username, iconUrl: ctx.User.AvatarUrl);

            await ctx.RespondAsync(eb);
        }
    }
}