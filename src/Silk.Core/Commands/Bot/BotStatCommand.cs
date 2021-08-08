#pragma warning disable CA1822 // Mark members as static

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using Humanizer.Localisation;
using Silk.Core.Utilities.HelpFormatter;

#pragma warning disable 1591

namespace Silk.Core.Commands.Bot
{
	[Category(Categories.Bot)]
	public class BotStatCommand : BaseCommandModule
	{
		[Command("botstats")]
		[Aliases("botinfo")]
		[Description("Get the current stats for Silk")]
		public async Task BotStat(CommandContext ctx)
		{
			using var process = Process.GetCurrentProcess();
			int guildCount = ctx.Client.Guilds.Count;
			int memberCount = ctx.Client.Guilds.Values.SelectMany(g => g.Members.Keys).Count();
			GC.Collect(2, GCCollectionMode.Forced, true, true);
			GC.WaitForPendingFinalizers();
			GC.Collect(2, GCCollectionMode.Forced, true, true);
			DiscordEmbedBuilder embed = new();
			embed
				.WithTitle("Stats for Silk!")
				.WithColor(DiscordColor.Gold)
				.AddField("Latency", $"{ctx.Client.Ping} ms", true)
				.AddField("Total Guilds", $"{guildCount}", true)
				.AddField("Total Members", $"{memberCount}", true)
				.AddField("Shards", $"{ctx.Client.ShardCount}", true)
				.AddField("Memory", $"{GC.GetTotalMemory(true) / 1024 / 1024:n2} MB", true)
				.AddField("Threads", $"{ThreadPool.ThreadCount}", true)
				.AddField("Uptime", (DateTime.Now - process.StartTime).Humanize(3, minUnit: TimeUnit.Second), true);
			await ctx.RespondAsync(embed);
		}
	}
}