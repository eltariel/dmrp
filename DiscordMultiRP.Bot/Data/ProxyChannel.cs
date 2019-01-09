namespace DiscordMultiRP.Bot.Data
{
    public class ProxyChannel
    {
        public int Id { get; set; }

        public Proxy Proxy { get; set; }
        public Channel Channel { get; set; }
    }
}