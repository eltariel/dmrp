using Discord;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Web.Models
{
    public class ChannelViewModel
    {
        public ChannelViewModel(Channel dbChannel, ITextChannel discordChannel)
        {
            DiscordId = discordChannel.Id;
            GuildName = discordChannel.Guild.Name;
            ChannelName = discordChannel.Name;

            if (dbChannel != null)
            {
                IsRegistered = true;
                IsMonitored = dbChannel.IsMonitored;
                DatabaseId = dbChannel.Id;
            }
            else
            {
                IsMonitored = false;
            }
        }

        public int? DatabaseId { get; }
        public ulong DiscordId { get; }

        public bool IsRegistered { get; set; }
        public bool IsMonitored { get; set; }

        public string GuildName { get; }
        public string ChannelName { get; }

        public string FullName => $"{GuildName}: {ChannelName}";
    }
}