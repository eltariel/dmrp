using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DiscordMultiRP.Web.Models
{
    public class ProxyCreateViewModel
    {
        public ProxyCreateViewModel() { }

        public ProxyCreateViewModel(ulong discordUserId, string name, string prefix, string suffix, string biography, bool isGlobal,
            List<ulong> channels, IFormFile avatar)
        {
            DiscordUserId = discordUserId;
            Name = name;
            Prefix = prefix;
            Suffix = suffix;
            Biography = biography;
            IsGlobal = isGlobal;
            Channels = channels;
            Avatar = avatar;
        }

        [Required]
        public ulong DiscordUserId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Biography { get; set; }
        public IFormFile Avatar { get; set; }

        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public bool IsGlobal { get; set; }
        public List<ulong> Channels { get; set; }

        // TODO: Make this less hacky.
        [MinLength(1)]
        public string DumbValidationHack => $"{(Prefix ?? "").Trim()}{(Suffix ?? "").Trim()}";
    }
}