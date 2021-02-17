﻿namespace Silk.Core.Services.Interfaces
{
    public delegate void GuildConfigUpdated(ulong id);

    public interface IServiceCacheUpdaterService
    {
        public event GuildConfigUpdated ConfigUpdated;

        public void UpdateGuild(ulong id);
    }

}