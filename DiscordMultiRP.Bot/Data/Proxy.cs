﻿using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace DiscordMultiRP.Bot.Data
{
    public class Proxy
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Guid AvatarGuid { get; set; }
        public string AvatarContentType { get; set; }

        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public string Biography { get; set; }

        public bool IsGlobal { get; set; }

        public ICollection<ProxyChannel> Channels { get; set; }
        
        public BotUser BotUser { get; set; }

        public bool IsForChannel(SocketMessage msg) =>
            IsGlobal ||
            Channels.Any(c => c.Channel.IsMonitored && c.Channel.DiscordId == msg.Channel.Id);

        public bool HasAvatar => AvatarGuid != Guid.Empty && !string.IsNullOrWhiteSpace(AvatarContentType);
    }
}