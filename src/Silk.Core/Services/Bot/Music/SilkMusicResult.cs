﻿using System;
using DSharpPlus.Entities;
using YoutubeExplode.Videos;

namespace Silk.Core.Services.Bot.Music
{
	public sealed class SilkMusicResult
	{
		public Video Video { get; init; }
		public DiscordUser RequestedBy { get; init; }
		public TimeSpan Duration { get; init; }
		
		public string AudioUrl { get; init; }
	}
}