﻿using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Silk.Core.Data.Models;

namespace Silk.Core.Data.MediatR.Guilds
{
    /// <summary>
    ///     Request for adding a <see cref="Guild" /> to the database.
    /// </summary>
    /// <param name="GuildId">The Id of the Guild</param>
    /// <param name="Prefix">The prefix for the Guild</param>
    public record AddGuildRequest(ulong GuildId, string Prefix) : IRequest<Guild>;

    /// <summary>
    ///     The default handler for <see cref="AddGuildRequest" />.
    /// </summary>
    public class AddGuildHandler : IRequestHandler<AddGuildRequest, Guild>
    {
        private readonly GuildContext _db;

        public AddGuildHandler(GuildContext db)
        {
            _db = db;
        }

        public async Task<Guild> Handle(AddGuildRequest request, CancellationToken cancellationToken)
        {
            Guild guild = new()
            {
                Id = request.GuildId,
                Prefix = request.Prefix,
                Configuration = new() { GuildId = request.GuildId },
                ModConfig = new() { GuildId = request.GuildId },
            };
            
            _db.Guilds.Add(guild);
            await _db.SaveChangesAsync(cancellationToken);
            
            return guild;
        }
    }
}