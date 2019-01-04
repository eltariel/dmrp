using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordMultiRP.Bot.Data
{
    public class User
    {
        public int Id { get; set; }

        public ulong DiscordId { get; set; }

        public ICollection<Proxy> Proxies { get; set; }

        public ICollection<UserChannel> Channels { get; set; }
    }
}
