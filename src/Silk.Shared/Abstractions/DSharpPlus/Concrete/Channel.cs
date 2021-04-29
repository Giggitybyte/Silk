﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Silk.Shared.Abstractions.DSharpPlus.Interfaces;

namespace Silk.Shared.Abstractions.DSharpPlus.Concrete
{
    /// <inheritdoc cref="IChannel"/>
    public class Channel : IChannel
    {
        public ulong Id => _channel.Id;
        public bool IsPrivate => _channel is DiscordDmChannel;

        public IGuild? Guild => (Guild?) _channel.Guild; // This gets cached, don't worry. //

        private readonly DiscordChannel _channel;
        private static readonly Dictionary<ulong, Channel> _channels = new();

        private Channel(DiscordChannel channel) => _channel = channel;

        public async Task<IMessage?> GetMessageAsync(ulong id) => (Message?) await _channel.GetMessageAsync(id);

        public static implicit operator Channel(DiscordChannel channel) => GetOrCacheChannel(channel);

        private static Channel GetOrCacheChannel(DiscordChannel channel)
        {
            if (_channels.TryGetValue(channel.Id, out var chn))
            {
                return chn;
            }
            else
            {
                chn = new(channel);
                _channels.Add(channel.Id, chn);
                return chn;
            }
        }
    }
}