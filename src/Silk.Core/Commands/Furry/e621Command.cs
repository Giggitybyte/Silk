﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Options;
using Silk.Core.Commands.Furry.Types;
using Silk.Core.Utilities.HelpFormatter;
using Silk.Shared.Configuration;

namespace Silk.Core.Commands.Furry
{
	[Category(Categories.Misc)]
	[ModuleLifespan(ModuleLifespan.Transient)]
	[Cooldown(1, 10, CooldownBucketType.User)]
	public class e621Command : eBooruBaseCommand
	{
		private readonly SilkConfigurationOptions _options;

		public e621Command(IHttpClientFactory httpClientFactory, IOptions<SilkConfigurationOptions> options) : base(httpClientFactory)
		{
			baseUrl = "https://e621.net/posts.json?tags=";
			_options = options.Value;
			username = _options.E621.Username;
		}

		[RequireNsfw]
		[Command("e621")]
		[Aliases("e6")]
		[Description("Lewd~ Get hot stuff of e621; requires channel to be marked as NSFW.")]
		public override async Task Search(CommandContext ctx, int amount = 1, [RemainingText] string? query = null)
		{
			if (query?.Split().Length > 5)
			{
				await ctx.RespondAsync("You can search 5 tags at a time!");
				return;
			}

			if (amount > 10)
			{
				await ctx.RespondAsync("You can only request 10 images every 10 seconds.");
				return;
			}

			eBooruPostResult? result;
			if (string.IsNullOrWhiteSpace(username))
				result = await DoQueryAsync(query); // May return empty results locked behind API key //
			else
				result = await DoKeyedQueryAsync(query, _options.E621.ApiKey, true);

			if (result?.Posts is null || result.Posts.Count is 0)
			{
				await ctx.RespondAsync("Seems like nothing exists by that search! Sorry! :(");
				return;
			}

			List<Post> posts = await GetPostsAsync(result, amount, (int)ctx.Message.Id);
			foreach (Post post in posts)
			{
				DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
					.WithTitle(query)
					.WithDescription($"[Direct Link]({post!.File.Url})\nDescription: {post!.Description.Truncate(200)}")
					.AddField("Score:", post.Score.Total.ToString())
					.AddField("Source:", GetSource(post.Sources.FirstOrDefault()?.ToString()) ?? "No source available")
					.WithColor(DiscordColor.PhthaloBlue)
					.WithImageUrl(post.File.Url)
					.WithFooter("Limit: 10 img / 10sec");

				await ctx.RespondAsync(embed);
				await Task.Delay(300);
			}
		}

		[Command("e621")]
		public async Task Search(CommandContext ctx, [RemainingText] string? search)
		{
			await Search(ctx, 3, search);
		}
	}
}