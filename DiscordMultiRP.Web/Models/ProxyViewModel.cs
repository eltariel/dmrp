using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using DiscordMultiRP.Bot.Data;
using Microsoft.AspNetCore.Http;

namespace DiscordMultiRP.Web.Models
{
    public class ProxyViewModel
    {
        public ProxyViewModel()
        {
        }

        public ProxyViewModel(Proxy proxy, string userName = null, IEnumerable<ITextChannel> discordChannels = null)
        {
            Id = proxy.Id;
            Name = proxy.Name;
            AvatarGuid = proxy.AvatarGuid;
            HasAvatar = proxy.AvatarGuid != Guid.Empty && !string.IsNullOrWhiteSpace(proxy.AvatarContentType);
            Prefix = proxy.Prefix;
            Suffix = proxy.Suffix;
            Biography = proxy.Biography;
            IsGlobal = proxy.IsGlobal;
            Channels = proxy.Channels?.Select(c => c.Channel.DiscordId).ToList();
            DbChannels = proxy.Channels;
            UserId = proxy.BotUser.Id;
            UserName = userName;
            UserDiscordId = proxy.BotUser.DiscordId;
            DiscordChannels = discordChannels?.ToList();
        }


        public int Id { get; set; }

        public string Name { get; set; }
        public bool HasAvatar { get; set; }
        public Guid AvatarGuid { get; set; }
        public IFormFile Avatar { get; set; }

        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public string Biography { get; set; }

        public bool IsGlobal { get; set; }

        public List<ulong> Channels { get; set; }
        public ICollection<ProxyChannel> DbChannels { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; }
        public ulong UserDiscordId { get; set; }
        public IEnumerable<ITextChannel> DiscordChannels { get; set; }
    }
}