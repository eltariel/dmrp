using Discord;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Web.Models
{
    public class UserViewModel
    {
        private readonly User db;
        private readonly IUser discord;

        public UserViewModel(User db, IUser discord)
        {
            this.db = db;
            this.discord = discord;
        }

        public int Id => db.Id;
        public ulong DiscordId => discord?.Id ?? db?.DiscordId ?? 0;
        public string Name => discord?.Username ?? $"[Unknown user]";
        public Role Role => db.Role;
    }
}