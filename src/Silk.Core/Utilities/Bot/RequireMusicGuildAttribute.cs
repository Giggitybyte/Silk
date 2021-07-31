using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Options;
using Silk.Extensions;
using Silk.Shared.Configuration;

namespace Silk.Core.Utilities.Bot
{
	public sealed class RequireMusicGuildAttribute : CheckBaseAttribute
	{
		public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			var options = ctx.Services.Get<IOptions<SilkConfigurationOptions>>()!.Value;

			return options.PrivateMusic?.Contains(ctx.Guild.Id) ?? false;
		}
	}
}