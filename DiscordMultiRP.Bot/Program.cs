using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordMultiRP.Bot.Dice;
using NLog;

namespace DiscordMultiRP.Bot
{
    class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly Logger dlog = LogManager.GetLogger(nameof(DiscordSocketClient));

        private readonly DiceRoller dice = new DiceRoller();

        private string botToken = "NTI4NDg5Njk5MjQxNjIzNTcy.DwjB-g.LRIL9dUGShnCOgLdmMhpKmro5gU";
        private DiscordSocketClient discord = new DiscordSocketClient();

        static async Task Main(string[] args)
        {
            log.Debug("Starting DiscordMultiRP");
            await new Program().Run(args);
        }

        private async Task Run(string[] args)
        {
            log.Debug("Client created");
            discord.Log += Log;
            discord.Ready += OnReady;
            discord.MessageReceived += OnMessage;

            await discord.LoginAsync(TokenType.Bot, botToken);
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
