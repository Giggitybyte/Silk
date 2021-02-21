﻿using MediatR;
using Silk.Data.Models;

namespace Silk.Data.MediatR
{
    public class GuildRequest
    {
        public record Add(ulong GuildId, string Prefix) : IRequest<Guild>;
        
        public record GetOrCreate(ulong GuildId, string Prefix) : IRequest<Guild>;
    }
}