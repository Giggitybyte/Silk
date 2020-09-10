﻿using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using SilkBot.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SilkBot.Bot;

namespace SilkBot.Commands.Bot
{
    public sealed class MessageCreationHandler
    {
        public MessageCreationHandler() => Instance.Client.MessageCreated += OnMessageCreate;

        private async Task OnMessageCreate(MessageCreateEventArgs e)
        {
            // Could use CommandTimer.Restart();
            CommandTimer.Restart();

            var config = Instance.SilkDBContext.Guilds.AsQueryable().FirstOrDefault(guild => guild.DiscordGuildId == e.Guild!.Id);
            if (e.Author.IsBot)
            {
                CommandTimer.Stop();
                return;
            }
            
            //if (e.Channel.IsPrivate) await CheckForTicket(e);
            //Using .GetAwaiter has results in ~50x performance because of async overhead.
            CheckForInvite(e, config);
            Console.WriteLine($"Scanned for an invite in message in {CommandTimer.ElapsedMilliseconds} ms.");
            
            var prefix = config?.Prefix ?? "!";
            var prefixPos = e.Message.GetStringPrefixLength(prefix);
            if (prefixPos < 1)
            {
                CommandTimer.Stop();
                return;
            }
            var pfx = e.Message.Content.Substring(0, prefixPos);
            var cnt = e.Message.Content.Substring(prefixPos);

            var cmd = Instance.Client.GetCommandsNext().FindCommand(cnt, out var args);
            var ctx = Instance.Client.GetCommandsNext().CreateContext(e.Message, pfx, cmd, args);
            if (cmd is null)
            {
                CommandTimer.Stop();
                return;
            }
            _ = Task.Run(async () => await Instance.Client.GetCommandsNext().ExecuteCommandAsync(ctx));
            CommandTimer.Stop();
        }

        private void CheckForInvite(MessageCreateEventArgs e, Guild config)
        {
            if (config.WhiteListInvites)
            {
                var messageContent = e.Message.Content;
                if (messageContent.Contains("discord.gg") || 
                    messageContent.Contains("discord.com/invite"))
                {
                    var inviteLinkMatched = Regex.Match(messageContent, @"(discord\.gg\/.+)") 
                                 ?? Regex.Match(messageContent.ToLower(), @"(discord\.com\/invite\/.+)");
                    
                    if (!inviteLinkMatched.Success)
                    {
                        return;
                    }

                    var inviteLink = string.Join("", messageContent
                        .Skip(inviteLinkMatched.Index)
                        .TakeWhile(c => c != ' '))
                        .Replace("discord.com/invite", "discord.gg/");
                    
                    if (!config.WhiteListedLinks.Any(link => link.Link == inviteLink))
                    {
                        e.Message.DeleteAsync().GetAwaiter();
                    }
                }
            }
        }

        private async Task CheckForTicket(MessageCreateEventArgs e)
        {
            var ticket = Instance.SilkDBContext.Tickets.AsQueryable().OrderBy(_ => _.Opened).LastOrDefault(ticketModel => ticketModel.Opener == e.Message.Author.Id);

            // Can use null-propagation because (default(IEnumerable) or reference type is null)
            if (ticket?.Responders == null)
            {
                return;
            }

            if (!e.Channel.IsPrivate)
            {
                return;
            }

            if (ticket.IsOpen && !ticket.Responders.Any(responder => responder.ResponderId == e.Message.Author.Id))
            {
                foreach (var responder in ticket.Responders.Select(r => r.ResponderId))
                {
                    await Instance.Client.PrivateChannels.Values
                        .FirstOrDefault(c => c.Users.Any(u => u.Id == responder))
                        .SendMessageAsync("yesn't");
                }
            }
        }
    }
}