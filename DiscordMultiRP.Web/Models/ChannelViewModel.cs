using Discord;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Web.Models
{
    public class ChannelViewModel
    {
        private readonly Channel dbChannel;
        private readonly ITextChannel discordChannel;

        public ChannelViewModel(Channel dbChannel, ITextChannel discordChannel)
        {
            this.dbChannel = dbChannel;
            this.discordChannel = discordChannel;
        }

        public ulong DiscordId => dbChannel.DiscordId;
        public bool IsMonitored => dbChannel.IsMonitored;
        public int Id => dbChannel.Id;
        public string Name => $"{discordChannel.Guild.Name}: {discordChannel.Name}";
    }
}