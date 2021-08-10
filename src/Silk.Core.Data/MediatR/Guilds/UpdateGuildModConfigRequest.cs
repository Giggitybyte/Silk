﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Silk.Core.Data.Models;

namespace Silk.Core.Data.MediatR.Guilds
{
	public record UpdateGuildModConfigRequest : IRequest
	{
		public UpdateGuildModConfigRequest(ulong guildId) => GuildId = guildId;
		public ulong GuildId { get; init; }
		
		public bool? ScanInvites { get; init; }
		public bool? BlacklistWords { get; init; }
		public bool? BlacklistInvites { get; init; }
		public bool? LogMembersJoining { get; init; }
		public bool? LogMembersLeaving { get; init; }
		public bool? UseAggressiveRegex { get; init; }
		public bool? WarnOnMatchedInvite { get; init; }
		public bool? DeleteOnMatchedInvite { get; init; }
		public int? MaxUserMentions { get; init; }
		public int? MaxRoleMentions { get; init; }
		public List<Invite>? AllowedInvites { get; init; }
		public List<InfractionStep>? InfractionSteps { get; init; }
		public ulong? MuteRoleId { get; init; }
		public ulong? LoggingChannel { get; init; }
		public bool? LogMessageChanges { get; init; }
	}
	
	public sealed class UpdateGuildModConfigHandler : IRequestHandler<UpdateGuildModConfigRequest>
	{
		private readonly GuildContext _db;
		public UpdateGuildModConfigHandler(GuildContext db) => _db = db;

		public async Task<Unit> Handle(UpdateGuildModConfigRequest request, CancellationToken cancellationToken)
		{
			var config = await _db.GuildModConfigs
				.FirstAsync(g => g.GuildId == request.GuildId, cancellationToken);
			
			config.MuteRoleId = request.MuteRoleId ?? config.MuteRoleId;
			config.LogMessageChanges = request.LogMessageChanges ?? config.LogMessageChanges;
			config.MaxUserMentions = request.MaxUserMentions ?? config.MaxUserMentions;
			config.MaxRoleMentions = request.MaxRoleMentions ?? config.MaxRoleMentions;
			config.InfractionSteps = request.InfractionSteps ?? config.InfractionSteps;
			config.AllowedInvites = request.AllowedInvites ?? config.AllowedInvites;
			config.LoggingChannel = request.LoggingChannel ?? config.LoggingChannel;
			config.ScanInvites = request.ScanInvites ?? config.ScanInvites;
			config.BlacklistWords = request.BlacklistWords ?? config.BlacklistWords;
			config.BlacklistInvites = request.BlacklistInvites ?? config.BlacklistInvites;
			config.LogMemberJoins = request.LogMembersJoining ?? config.LogMemberJoins;
			config.LogMemberLeaves = request.LogMembersLeaving ?? config.LogMemberLeaves;
			config.UseAggressiveRegex = request.UseAggressiveRegex ?? config.UseAggressiveRegex;
			config.WarnOnMatchedInvite = request.WarnOnMatchedInvite ?? config.WarnOnMatchedInvite;
			config.DeleteMessageOnMatchedInvite = request.DeleteOnMatchedInvite ?? config.DeleteMessageOnMatchedInvite;

			await _db.SaveChangesAsync(cancellationToken);
			
			return Unit.Value;
		}
	}
}