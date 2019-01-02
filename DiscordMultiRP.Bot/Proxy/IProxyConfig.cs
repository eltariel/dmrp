namespace DiscordMultiRP.Bot.Proxy
{
    public interface IProxyConfig
    {
        User GetUserById(ulong userId);
    }
}