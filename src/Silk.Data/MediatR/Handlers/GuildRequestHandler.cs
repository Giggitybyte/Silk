﻿using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Silk.Data.Models;

namespace Silk.Data.MediatR.Handlers
{
    public class GuildRequestHandler
    {
        public class GuildAddRequestHandler : IRequestHandler<GuildRequest.AddGuildRequest, Guild>
        {
            private readonly SilkDbContext _db;

            public GuildAddRequestHandler(SilkDbContext db)
            {
                _db = db;
            }

            public async Task<Guild> Handle(GuildRequest.AddGuildRequest request, CancellationToken cancellationToken)
            {
                var guild = new Guild {Id = request.GuildId, Configuration = new(), Prefix = request.Prefix};
                await _db.SaveChangesAsync(cancellationToken);
                return guild;
            }
        }
        
        
        public class GuildGetOrCreateRequestHandler : IRequestHandler<GuildRequest.GetOrCreateGuildRequest, Guild>
        {
            private readonly SilkDbContext _db;

            public GuildGetOrCreateRequestHandler(SilkDbContext db)
            {
                _db = db;
            }
        
            public async Task<Guild> Handle(GuildRequest.GetOrCreateGuildRequest request, CancellationToken cancellationToken)
            {
                Guild? guild = await _db.Guilds
                    .Include(g => g.Users)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(g => g.Id == request.GuildId, cancellationToken);

                if (guild is not null) return guild;

                guild = new()
                {
                    Id = request.GuildId,
                    Users = new(),
                    Prefix = request.Prefix,
                    Configuration = new() { GuildId = request.GuildId }
                };
                _db.Guilds.Add(guild);
                await _db.SaveChangesAsync(cancellationToken);

                return guild;
            }
        }
    }
}