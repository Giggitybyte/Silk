﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using MediatR;
using NpgsqlTypes;
using Silk.Core.Data.MediatR.Guilds;
using Silk.Core.Data.MediatR.Guilds.Config;
using Silk.Core.Data.Models;
using Silk.Core.Services.Interfaces;
using Silk.Core.Utilities;
using Silk.Extensions;
using Silk.Extensions.DSharpPlus;
using Silk.Shared.Constants;

namespace Silk.Core.Commands
{
	[RequireGuild]
	[Group("config")]
	[RequireFlag(UserFlag.Staff)]
	[Description("View and edit configuration for the current guild.")]
	public class ConfigModule : BaseCommandModule
	{
		private readonly ICacheUpdaterService _updater;
		public ConfigModule(ICacheUpdaterService updater) => _updater = updater;

		[Command]
		[Description("Reloads the config from the database. May temporarily slow down response time. (Configs are automatically reloaded every 10 minutes!)")]
		public async Task Reload(CommandContext ctx)
		{
			var res = await EditConfigModule.GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

			if (!res) return;
			
			_updater.UpdateGuild(ctx.Guild.Id);
		}
		
		// Wrapper that points to config view //
		[GroupCommand]
		public Task Default(CommandContext ctx) =>
			ctx.CommandsNext.ExecuteCommandAsync(ctx.CommandsNext.CreateContext(ctx.Message, ctx.Prefix, ctx.CommandsNext.RegisteredCommands["config view"]));
		
		[Group("view")]
		public sealed class ViewConfigModule : BaseCommandModule
		{
			private readonly IMediator _mediator;
			public ViewConfigModule(IMediator mediator) => _mediator = mediator;

			private string GetCountString(int count) => count is 0 ? "Not set/enabled" : count.ToString();

			[GroupCommand]
			[Description("View the current config.")]
			public async Task View(CommandContext ctx)
			{
				var config = await _mediator.Send(new GetGuildConfigRequest(ctx.Guild.Id));
				var modConfig = await _mediator.Send(new GetGuildModConfigRequest(ctx.Guild.Id));

				var embed = new DiscordEmbedBuilder();
				var contentBuilder = new StringBuilder();
				
				contentBuilder
					.Clear()
					.AppendLine("**General Config:**")
					.AppendLine("__Greeting:__")
					.AppendLine($"> Option: {config.GreetingOption.Humanize()}")
					.AppendLine($"> Greetting channel {(config.GreetingOption is GreetingOption.DoNotGreet ? "N/A" : $"<#{config.GreetingChannel}>")}")
					.AppendLine($"> Greeting text: {(config.GreetingOption is GreetingOption.DoNotGreet ? "N/A" : $"[See {ctx.Prefix}config view greeting]")}")
					.AppendLine()
					.AppendLine()
					.AppendLine("**Moderation Config:**")
					.AppendLine()
					.AppendLine("__Logging:__")
					.AppendLine($"> Channel: {(modConfig.LoggingChannel is var channel and not 0 ? $"<#{channel}>" : "Not set")}")
					.AppendLine($"> Log members joining: <:_:{(modConfig.LogMemberJoins ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($"> Log members leaving: <:_:{(modConfig.LogMemberLeaves ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($"> Log message edits/deletions: <:_:{(modConfig.LogMessageChanges ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine()
					.AppendLine($"Max role mentions: {GetCountString(modConfig.MaxRoleMentions)}")
					.AppendLine($"Max user mentions: {GetCountString(modConfig.MaxUserMentions)}")
					.AppendLine()
					.AppendLine("__Invites:__")
					.AppendLine($"> Scan invite: <:_:{(modConfig.ScanInvites ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($"> Infract on invite: <:_:{(modConfig.WarnOnMatchedInvite ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($"> Delete matched invite: <:_:{(modConfig.DeleteMessageOnMatchedInvite ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($@"> Use agressive invite matching: <:_:{(modConfig.UseAggressiveRegex ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($"> Allowed invites: {(modConfig.AllowedInvites.Count is 0 ? "None" : $"{modConfig.AllowedInvites.Count} allowed invites [See {ctx.Prefix}config view invites]")}")
					.AppendLine("Aggressive pattern matching regex:")
					.AppendLine(@"`disc((ord)?(((app)?\.com\/invite)|(\.gg)))\/([A-z0-9-_]{2,})`")
					.AppendLine()
					.AppendLine("__Infractions:__")
					.AppendLine($"> Mute role: {(modConfig.MuteRoleId is 0 ? "Not set" : $"<@&{modConfig.MuteRoleId}>")}")
					.AppendLine($"> Infraction steps: {(modConfig.InfractionSteps.Count is var dictCount and not 0 ? $"{dictCount} steps [See {ctx.Prefix}config view infractions]" : "Not configured")}")
					.AppendLine($"> Infraction steps (named): {((modConfig.NamedInfractionSteps?.Count ?? 0) is var infNameCount and not 0 ? $"{infNameCount} steps [See {ctx.Prefix}config view infractions]" : "Not configured")}")
					.AppendLine($"> Auto-escalate automod infractions: <:_:{(modConfig.AutoEscalateInfractions ? Emojis.ConfirmId : Emojis.DeclineId)}>");
				
				embed
					.WithTitle($"Configuration for {ctx.Guild.Name}:")
					.WithColor(DiscordColor.Azure)
					.WithDescription(contentBuilder.ToString());
				
				await ctx.RespondAsync(embed);
			}
			
			// Justification for ommiting a Log command in the View group:			//
			// The commands below exist because they house complex information		//
			// that would otherwise bloat the main embed to > 4096 characters,		//
			// which is the limit for embed descriptions. Log however only houses	//
			// A few booleans, and thus does not need it's own command in the view	//
			// group.																//
			
			[Command]
			[Description("View in-depth greeting-related config.")]
			public async Task Greeting(CommandContext ctx)
			{
				var contentBuilder = new StringBuilder();
				var config = await _mediator.Send(new GetGuildConfigRequest(ctx.Guild.Id));

				contentBuilder
					.Clear()
					.AppendLine("__**Greeting option:**__")
					.AppendLine($"> Option: {config.GreetingOption.Humanize()}")
					.AppendLine($"> Greetting channel {(config.GreetingOption is GreetingOption.DoNotGreet ? "N/A" : $"<#{config.GreetingChannel}>")}")
					.AppendLine($"> Greeting text: {(config.GreetingOption is GreetingOption.DoNotGreet ? "N/A" : $"\n\n{config.GreetingText}")}")
					.AppendLine($"> Greeting role: {(config.GreetingOption is GreetingOption.GreetOnRole && config.VerificationRole is var role and not 0 ? $"<@&{role}>" : "N/A")}");

				var explanation = config.GreetingOption switch
				{
					GreetingOption.DoNotGreet => "I will not greet members at all.",
					GreetingOption.GreetOnJoin => "I will greet members as soon as they join",
					GreetingOption.GreetOnRole => "I will greet members when they're given a specific role",
					GreetingOption.GreetOnScreening => "I will greet members when they pass membership screening. Only applicable to community servers.",
					_ => throw new ArgumentOutOfRangeException()
				};

				contentBuilder
					.AppendLine()
					.AppendLine("**Greeting option explanation:**")
					.AppendLine(explanation);

				var embed = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Azure)
					.WithTitle($"Config for {ctx.Guild.Name}")
					.WithDescription(contentBuilder.ToString());

				await ctx.RespondAsync(embed);
			}

			[Command]
			[Description("View in-depth invite related config.")]
			public async Task Invites(CommandContext ctx)
			{
				//TODO: config view invites-list
				var config = await _mediator.Send(new GetGuildModConfigRequest(ctx.Guild.Id));
				var contentBuilder = new StringBuilder();

				contentBuilder
					.Clear()
					.AppendLine("__Invites:__")
					.AppendLine($"> Scan invite: <:_:{(config.ScanInvites ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($"> Infract on invite: <:_:{(config.WarnOnMatchedInvite ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($"> Delete matched invite: <:_:{(config.DeleteMessageOnMatchedInvite ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine($@"> Use agressive invite matching : <:_:{(config.UseAggressiveRegex ? Emojis.ConfirmId : Emojis.DeclineId)}>")
					.AppendLine()
					.AppendLine($"> Allowed invites: {(config.AllowedInvites.Count is 0 ? "There are no whitelisted invites!" : $"{config.AllowedInvites.Count} allowed invites:")}")
					.AppendLine($"> {config.AllowedInvites.Take(15).Select(inv => $"`{inv.VanityURL}`\n").Join("> ")}");

				if (config.AllowedInvites.Count > 15)
					contentBuilder.AppendLine($"..Plus {config.AllowedInvites.Count - 15} more");
				
				contentBuilder
					.AppendLine("Aggressive pattern matching are any invites that match this rule:")
					.AppendLine(@"`disc((ord)?(((app)?\.com\/invite)|(\.gg)))\/([A-z0-9-_]{2,})`");

				var embed = new DiscordEmbedBuilder()
					.WithTitle($"Configuration for {ctx.Guild.Name}:")
					.WithColor(DiscordColor.Azure)
					.WithDescription(contentBuilder.ToString());

				await ctx.RespondAsync(embed);
			}

			[Command]
			[Description("View in-depth infraction-ralated config.")]
			public async Task Infractions(CommandContext ctx)
			{
				var config = await _mediator.Send(new GetGuildModConfigRequest(ctx.Guild.Id));
				
				var contentBuilder = new StringBuilder()
					.AppendLine("__Infractions:__")
					.AppendLine($"> Infraction steps: {(config.InfractionSteps.Count is var dictCount and not 0 ? $"{dictCount} steps" : "Not configured")}")
					.AppendLine($"> Infraction steps (named): {((config.NamedInfractionSteps?.Count ?? 0) is var infNameCount and not 0 ? $"{infNameCount} steps" : "Not configured")}")
					.AppendLine($"> Auto-escalate automod infractions: <:_:{(config.AutoEscalateInfractions ? Emojis.ConfirmId : Emojis.DeclineId)}>");

				if (config.InfractionSteps.Any())
				{
					contentBuilder
						.AppendLine()
						.AppendLine("Infraction steps:")
						.AppendLine(config.InfractionSteps.Select((inf, count) => $"` {count + 1} ` strikes -> {inf.Type} {(inf.Duration == NpgsqlTimeSpan.Zero ? "" : $"For {inf.Duration.Time.Humanize()}")}").Join("\n"));
				}
				
				if (config.NamedInfractionSteps?.Any() ?? false)
				{
					contentBuilder
					.AppendLine()
					.AppendLine("Auto-Mod action steps:")
					.AppendLine(config.NamedInfractionSteps.Select(inf => $"`{inf.Key}` -> {inf.Value.Type} {(inf.Value.Duration == NpgsqlTimeSpan.Zero ? "" : $"For {inf.Value.Duration.Time.Humanize()}")}").Join("\n"));
				}
				
				var embed = new DiscordEmbedBuilder()
					.WithTitle($"Configuration for {ctx.Guild.Name}:")
					.WithColor(DiscordColor.Azure)
					.WithDescription(contentBuilder.ToString());

				await ctx.RespondAsync(embed);
			}
		}

		[Group("edit")]
		public sealed class EditConfigModule : BaseCommandModule
		{
			// Someone's gonna chew me a new one with this many statics lmao //
			private static readonly DiscordButtonComponent _yesButton = new(ButtonStyle.Success, "confirm action", null, false, new(Emojis.ConfirmId));
			private static readonly DiscordButtonComponent _noButton = new(ButtonStyle.Danger, "decline action", null, false, new(Emojis.DeclineId));

			private static readonly DiscordButtonComponent _yesButtonDisabled = new DiscordButtonComponent(_yesButton).Disable();
			private static readonly DiscordButtonComponent _noButtonDisabled = new DiscordButtonComponent(_noButton).Disable();

			private static readonly DiscordInteractionResponseBuilder _confirmBuilder = new DiscordInteractionResponseBuilder().WithContent("Alright!").AddComponents(_yesButtonDisabled, _noButtonDisabled);
			private static readonly DiscordInteractionResponseBuilder _declineBuilder = new DiscordInteractionResponseBuilder().WithContent("Cancelled!").AddComponents(_yesButtonDisabled, _noButtonDisabled);

			private readonly IMediator _mediator;
			private static readonly ConcurrentDictionary<ulong, CancellationTokenSource> _tokens = new();
			public EditConfigModule(IMediator mediator) => _mediator = mediator;
			
			
			[Command]
			[Description("Edit the mute role to give to members when muting. If this isn't configured, one will be generated as necessary.")]
			public async Task Mute(CommandContext ctx, DiscordRole role)
			{
				var notMuteRole = role.Permissions.HasPermission(Permissions.SendMessages);
				var canChangeMuteRole = ctx.Guild.CurrentMember.HasPermission(Permissions.ManageRoles);
				var roleTooHigh = ctx.Guild.CurrentMember.Roles.Max(r => r.Position) <= role.Position;

				if (notMuteRole)
				{
					var msg = (canChangeMuteRole, roleTooHigh) switch
					{
						(true, false) => "",
						(true, true) => "That role is too high and has permission to send messages! Please fix this and try again.",
						(false, true) => "I don't have permission to edit this role, and it has permission to send messages! Please fix this and try again.",
						(false, false) => "This role has permission to send messages, and I can't edit it. Please fix this and try again."
					};

					if (!canChangeMuteRole || roleTooHigh)
					{
						await ctx.RespondAsync(msg);
						return;
					}
					else
					{
						await role.ModifyAsync(m => m.Permissions = role.Permissions ^ Permissions.SendMessages);
					}
				}
					
				EnsureCancellationTokenCancellation(ctx.User.Id);

				var res = await GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

				if (!res) return;

				await _mediator.Send(new UpdateGuildModConfigRequest(ctx.Guild.Id) { MuteRoleId = role.Id });
			}
			
			[Command]
			[Aliases("welcome")]
			[Description("Edit whether or not I greet members\nOptions: \n\n`role` -> greet on role, \n`join` -> greet on join, \n`disable` -> disable greetings \n`screening` -> greet when membership screening is passed")]
			public async Task Greeting(CommandContext ctx, string option)
			{
				var parsedOption = option.ToLower() switch
				{
					"disable" => GreetingOption.DoNotGreet,
					"role" => GreetingOption.GreetOnRole,
					"join" => GreetingOption.GreetOnJoin,
					"screening" => GreetingOption.GreetOnScreening,
					_ => (GreetingOption) (-1)
				};

				if ((int)parsedOption is -1)
				{
					await ctx.RespondAsync("That doesn't appear to be a valid option!");
					return;
				}
				
				EnsureCancellationTokenCancellation(ctx.User.Id);
				
				var res = await GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

				if (!res) return;

				await _mediator.Send(new UpdateGuildConfigRequest(ctx.Guild.Id) { GreetingOption = parsedOption });
			}

			[Command]
			[Aliases("greeting-channel", "welcomechannel", "welcome-channel", "gc", "wc")]
			public async Task GreetingChannel(CommandContext ctx, DiscordChannel channel)
			{
				var conf = await _mediator.Send(new GetGuildConfigRequest(ctx.Guild.Id));

				if (string.IsNullOrEmpty(conf.GreetingText))
				{
					await ctx.RespondAsync("Set a welcome message first!");
					return;
				}
				
				EnsureCancellationTokenCancellation(ctx.User.Id);

				var res = await GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

				if (!res) return;

				await _mediator.Send(new UpdateGuildConfigRequest(ctx.Guild.Id) { GreetingChannelId = channel.Id });
			}

			[Command]
			[Aliases("greeting-channel","welcomemessage", "wm", "gm")]
			public async Task GreetingMessage(CommandContext ctx, [RemainingText] string message)
			{
				if (message.Length > 2000)
				{
					await ctx.RespondAsync("Welcome message must be 2000 characters or less!");
					return;
				}

				if (string.IsNullOrEmpty(message))
				{
					await ctx.RespondAsync("You must provide a message!");
					return;
				}
				
				EnsureCancellationTokenCancellation(ctx.User.Id);

				var res = await GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

				if (!res) return;

				await _mediator.Send(new UpdateGuildConfigRequest(ctx.Guild.Id) { GreetingText = message });
			}
			
			
			[Group("log")]
			public sealed class EditLogModule : BaseCommandModule
			{
				private readonly IMediator _mediator;
				public EditLogModule(IMediator mediator) => _mediator = mediator;
				
				[Command]
				[Description("Edit the channel I logs infractions, users, etc to!")]
				public async Task Channel(CommandContext ctx, DiscordChannel channel)
				{
					if (!channel.PermissionsFor(ctx.Guild.CurrentMember).HasPermission(FlagConstants.LoggingPermissions))
					{
						await ctx.RespondAsync($"I don't have proper permissions to log there! I need {FlagConstants.LoggingPermissions.ToPermissionString()}");
						return;
					}
				
					EnsureCancellationTokenCancellation(ctx.User.Id);
				
					var res = await GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

					if (!res) return;

					await _mediator.Send(new UpdateGuildModConfigRequest(ctx.Guild.Id) { LoggingChannel = channel.Id });
				}
				
				[Command("member-joins")]
				[Aliases("members-joining", "mj")]
				[Description("Edit whether or not I log members that join")]
				public async Task MembersJoin(CommandContext ctx, bool log)
				{
					EnsureCancellationTokenCancellation(ctx.User.Id);

					var res = await GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

					if (!res) return;

					await _mediator.Send(new UpdateGuildModConfigRequest(ctx.Guild.Id) { LogMembersJoining = log });
				}
				
				[Command("member-leaves")]
				[Aliases("members-leaving", "ml")]
				[Description("Edit whether or not I log members that leave")]
				public async Task MembersLeave(CommandContext ctx, bool log)
				{
					EnsureCancellationTokenCancellation(ctx.User.Id);

					var res = await GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

					if (!res) return;

					await _mediator.Send(new UpdateGuildModConfigRequest(ctx.Guild.Id) { LogMembersLeaving = log });
				}

				[Command("message-edits")]
				[Description("Whether or not I log message edits and deletions. Requires a log channel to be set.")]
				public async Task MessageEdits(CommandContext ctx, bool log)
				{
					EnsureCancellationTokenCancellation(ctx.User.Id);

					var res = await GetButtonConfirmationUserInputAsync(ctx.User, ctx.Channel);

					if (!res) return;

					await _mediator.Send(new UpdateGuildModConfigRequest(ctx.Guild.Id) { LogMessageChanges = log });
				}
				
			}
			
			
			
			/// <summary>
			/// Waits indefinitely for user confirmation unless the associated token is cancelled.
			/// </summary>
			/// <param name="user">The id of the user to assign a token to and wait for input from.</param>
			/// <param name="channel">The channel to send a message to, to request user input.</param>
			/// <returns>True if the user selected true, or false if the user selected no OR the cancellation token was cancelled.</returns>
			internal static async Task<bool> GetButtonConfirmationUserInputAsync(DiscordUser user, DiscordChannel channel)
			{
				var builder = new DiscordMessageBuilder().WithContent("Are you sure?").AddComponents(_yesButton, _noButton);

				var message = await builder.SendAsync(channel);
				var token = GetTokenFromWaitQueue(user.Id);
				
				var interactivityResult = await channel.GetClient().GetInteractivity().WaitForButtonAsync(message, user, token);

				if (interactivityResult.TimedOut) // CT was yeeted. //
				{
					await message.ModifyAsync(b => b.WithContent("Cancelled!").AddComponents(_yesButtonDisabled, _noButtonDisabled));
					return false;
				}
				
				// Nobody likes 'This interaction failed'. //
				if (interactivityResult.Result.Id == _yesButton.CustomId)
				{
					await interactivityResult.Result
						.Interaction
						.CreateResponseAsync(InteractionResponseType.UpdateMessage, _confirmBuilder);
					
					return true;
				}
				else
				{
					await interactivityResult.Result
						.Interaction
						.CreateResponseAsync(InteractionResponseType.UpdateMessage, _declineBuilder);
					
					return false;
				}
			}
			
			/// <summary>
			/// Cancels and removes the token with the specified id if it exists.
			/// </summary>
			/// <param name="id">The id of the user to look up.</param>
			private static void EnsureCancellationTokenCancellation(ulong id)
			{
				if (_tokens.TryRemove(id, out var token))
				{
					token.Cancel();
					token.Dispose();
				}
			}
			
			/// <summary>
			/// Gets a <see cref="CancellationToken"/>, creating one if necessary.
			/// </summary>
			/// <param name="id">The id of the user to assign the token to.</param>
			/// <returns>The returned or generated token.</returns>
			private static CancellationToken GetTokenFromWaitQueue(ulong id) => _tokens.GetOrAdd(id, id => _tokens[id] = new()).Token;
		}
	}
}