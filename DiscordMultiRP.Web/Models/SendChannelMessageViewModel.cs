namespace DiscordMultiRP.Web.Models
{
    public class SendChannelMessageViewModel
    {
        public ulong ChannelDiscordId { get; set; }

        public int ChannelDatabaseId { get; set; }

        public string ChannelName { get; set; }

        public string GuildName { get; set; }

        public string Message { get; set; }

        public string UserName { get; set; }

        public string AvatarUri { get; set; }
    }
}