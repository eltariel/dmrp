using System.Collections.Generic;
using System.Linq;
using DiscordMultiRP.Bot.Data;
using Microsoft.AspNetCore.Http;

namespace DiscordMultiRP.Web.Models
{
    public class ProxyViewModel
    {
        public ProxyViewModel()
        {
        }

        public ProxyViewModel(Proxy proxy, string userName = null)
        {
            Id = proxy.Id;
            Name = proxy.Name;
            //Avatar = proxy.Avatar;
            Prefix = proxy.Prefix;
            Suffix = proxy.Suffix;
            IsReset = proxy.IsReset;
            IsGlobal = proxy.IsGlobal;
            Channels = proxy.Channels?.Select(c => c.Channel.DiscordId).ToList();
            UserId = proxy.User.Id;
            UserName = userName;
            UserDiscordId = proxy.User.DiscordId;
        }

        public int Id { get; set; }

        public string Name { get; set; }
        public IFormFile Avatar { get; set; }

        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public bool IsReset { get; set; }
        public bool IsGlobal { get; set; }

        public List<ulong> Channels { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; }
        public ulong UserDiscordId { get; set; }
    }
}