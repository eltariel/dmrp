using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordMultiRP.Bot.Data;
using DiscordMultiRP.Bot.Dice;
using DiscordMultiRP.Bot.ProxyResponder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DiscordMultiRP.Bot
{
    public class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly Logger dlog = LogManager.GetLogger(nameof(DiscordSocketClient));

        private readonly IConfiguration cfg;
        private readonly DiscordSocketClient discord = new DiscordSocketClient();

        private readonly DiceRoller dice = new DiceRoller();
        private UserProxy proxy;

        public static async Task Main(string[] args)
        {
            var cfg = new ConfigurationBuilder()
                .AddJsonFile(j =>
                {
                    j.Path = "appsettings.json";
                    j.Optional = true;
                })
                .AddEnvironmentVariables()
                .Build();
            log.Debug("Starting DiscordMultiRP");
            await new Program(cfg).Run();
        }

        private Program(IConfiguration cfg)
        {
            this.cfg = cfg;
        }

        private async Task Run()
        {
            log.Debug("Client created");

            var pc = new EfProxyBuilder(cfg);

            proxy = new UserProxy(discord, pc, cfg);

            discord.Log += Log;
            discord.Ready += OnReady;
            discord.MessageReceived += OnMessage;

            await discord.LoginAsync(TokenType.Bot, cfg["Discord:bot-token"]);
            log.Debug("Login complete");
            await discord.StartAsync();
            log.Debug("Started");

            await Task.Delay(Timeout.Infinite);
        }

        private Task Stop()
        {
            log.Info("Stop requested.");
            return discord.LogoutAsync();
        }

        private Task OnReady()
        {
            log.Info("DiscordMultiRP bot online.");
            return Task.CompletedTask;
        }

        private Task OnMessage(SocketMessage msg)
        {
            log.Debug($"Message  [{msg}] from [{msg.Author} ({(msg.Author.IsBot ? "Bot" : "Not bot")}|{(msg.Author.IsWebhook ? "Webhook" : "Not Webhook")})] in [{msg.Channel}]");
            Task.Run(async () =>
            {
                var text = msg.Content;
                if (msg.Author.IsBot) return;

                log.Debug($"Handling [{msg}] from [{msg.Author} ({(msg.Author.IsBot ? "Bot" : "Not bot")}|{(msg.Author.IsWebhook ? "Webhook" : "Not Webhook")})] in [{msg.Channel}]");
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
