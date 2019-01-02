using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordMultiRP.Bot.Dice;
using DiscordMultiRP.Bot.Proxy;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DiscordMultiRP.Bot
{
    class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly Logger dlog = LogManager.GetLogger(nameof(DiscordSocketClient));

        private readonly IConfiguration cfg;
        private readonly DiscordSocketClient discord = new DiscordSocketClient();

        private readonly DiceRoller dice = new DiceRoller();
        private UserProxy proxy;

        private Program(IConfiguration cfg)
        {
            this.cfg = cfg;
        }

        public static async Task Main(string[] args)
        {
            var cfg = new ConfigurationBuilder()
                .AddEnvironmentVariables("DMRP-")
                .AddJsonFile(j =>
                {
                    j.Path = "appsettings.json";
                    j.Optional = true;
                })
                .Build();
            log.Debug("Starting DiscordMultiRP");
            await new Program(cfg).Run();
        }

        private async Task Run()
        {
            log.Debug("Client created");

            proxy = new UserProxy(discord, new AppSettingsProxyConfig(cfg.GetSection("users")));

            discord.Log += Log;
            discord.Ready += OnReady;
            discord.MessageReceived += OnMessage;

            await discord.LoginAsync(TokenType.Bot, cfg["bot-token"]);
            log.Debug("Login complete");
            await discord.StartAsync();
            log.Debug("Started");

            await Task.Delay(Timeout.Infinite);
        }

        private Task OnReady()
        {
            log.Info("DiscordMultiRP bot online.");
            return Task.CompletedTask;
        }

        private  Task OnMessage(SocketMessage msg)
        {
            Task.Run(async () =>
            {
                var text = msg.Content;
                if (msg.Author.IsBot) return;

                await dice.HandleRolls(msg.Channel, text, msg.Author.Id);
                await proxy.HandleMessage(msg);
            });

            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            dlog.Debug(msg.ToString);
            return Task.CompletedTask;
        }
    }
}
