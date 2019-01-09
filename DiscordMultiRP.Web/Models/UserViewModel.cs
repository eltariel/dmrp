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
        public ulong DiscordId => discord.Id;
        public string Name => discord.Username;
        public Role Role => db.Role;
    }
}