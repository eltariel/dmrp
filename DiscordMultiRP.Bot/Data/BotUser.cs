using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordMultiRP.Bot.Data
{
    public enum Role
    {
        None = 0,
        User   = 1 << 0,
        Admin  = 1 << 1,
        Global = 1 << 2,
    }

    public class BotUser
    {
        public int Id { get; set; }

        public ulong DiscordId { get; set; }

        public Role Role { get; set; }

        public string ResetCommand { get; set; }

        public Proxy LastGlobalProxy { get; set; }

        public ICollection<Proxy> Proxies { get; set; }

        public ICollection<UserChannel> Channels { get; set; }

        public bool IsAllowedGlobal => Role == Role.Admin || Role == Role.Global;

        public bool IsAdmin => Role == Role.Admin;
    }
}
