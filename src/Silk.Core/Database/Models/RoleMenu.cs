﻿using System.Collections.Generic;

namespace Silk.Core.Database.Models
{
    public class RoleMenu
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ulong OwnerId { get; set; }
        public GuildConfigModel Guild { get; set; }
        public ulong MessageId { get; set; }
        public List<RoleMenuReactionModel> Reactions { get; set; } = new();
    }
}