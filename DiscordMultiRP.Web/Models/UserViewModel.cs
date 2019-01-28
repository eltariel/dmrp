using Discord;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Web.Models
{
    public class UserViewModel
    {
        private readonly BotUser botUser;
        private readonly IUser discordUser;

        public UserViewModel(BotUser botUser, IUser discordUser)
        {
            this.botUser = botUser;
            this.discordUser = discordUser;
        }

        public int Id => botUser.Id;
        public ulong DiscordId => discordUser?.Id ?? botUser?.DiscordId ?? 0;
        public string Name => discordUser?.Username ?? $"[Unknown user]";
        public Role Role => botUser.Role;
    }
}