﻿using System;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Silk.Shared.Constants;

namespace Silk.Core.Utilities
{
    /// <summary>
    ///     A class that holds the base configurations for various Discord-related entities.
    /// </summary>
    public static class DiscordConfigurations
    {
        /// <summary>
        ///     The base configuration used for <see cref="DiscordShardedClient" />.
        /// </summary>
        public static DiscordConfiguration Discord { get; } = new()
        {
            Intents = FlagConstants.Intents,
            LogTimestampFormat = "h:mm:ss ff tt",
            MessageCacheSize = 2048,
            LargeThreshold = 10000,
            MinimumLogLevel = LogLevel.None,
            LoggerFactory = new SerilogLoggerFactory()
        };

        /// <summary>
        ///     The base configuration used for <see cref="InteractivityExtension" />.
        /// </summary>
        public static InteractivityConfiguration Interactivity { get; } = new()
        {
            PaginationBehaviour = PaginationBehaviour.WrapAround,
            PaginationDeletion = PaginationDeletion.DeleteMessage,
            PollBehaviour = PollBehaviour.DeleteEmojis,
            Timeout = TimeSpan.FromMinutes(1)
        };

        public static CommandsNextConfiguration CommandsNext { get; } = new()
        {
            IgnoreExtraArguments = true,
            UseDefaultCommandHandler = false,
        };
    }
}