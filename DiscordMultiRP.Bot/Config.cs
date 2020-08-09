using Discord.Net.Rest;
using Discord.WebSocket;

namespace DiscordMultiRP.Bot
{
    public static class Config
    {
        public static readonly string ApiBaseUrl = "https://discord.com/api/";

        public static readonly DiscordSocketConfig DiscordConfig = new DiscordSocketConfig
        {
            RestClientProvider = url =>
                DefaultRestClientProvider.Instance(ApiBaseUrl + $"v{Discord.DiscordConfig.APIVersion}/")
        };
    }
}