using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DiscordMultiRP.Bot.Data;
using DiscordMultiRP.Web.Models;
using DiscordMultiRP.Web.Util;
using Microsoft.AspNetCore.Authorization;
using DiscordMultiRP.Bot.ProxyResponder;
using Microsoft.Extensions.Configuration;
using Discord.Webhook;
using Microsoft.AspNetCore.Authentication;

namespace DiscordMultiRP.Web.Controllers
{
    [RequireDiscord]
    public class ChannelsController : Controller
    {
        private const int MESSAGE_FOR_BOT = -1;
        private readonly ProxyDataContext db;
        private readonly DiscordHelper discordHelper;
        private readonly IConfiguration cfg;
        private readonly WebhookCache webhookCache;

        public ChannelsController(ProxyDataContext db, DiscordHelper discordHelper, IConfiguration cfg, WebhookCache webhookCache)
        {
            this.db = db;
            this.discordHelper = discordHelper;
            this.cfg = cfg;
            this.webhookCache = webhookCache;
        }

        // GET: Channels
        public async Task<IActionResult> Index()
        {
            var discord = await discordHelper.LoginBot();

            var discordChannels = discord.Guilds
                .SelectMany(g => g.TextChannels)
                .OrderBy(c => c.Guild.Name)
                .ThenBy(c => c.Name);
            var dbChannels = await db.Channels.ToListAsync();
            var channels = discordChannels.Select(c =>
                new ChannelViewModel(dbChannels.FirstOrDefault(d => d.DiscordId == c.Id), c));

            return View(channels);
        }

        // GET: Channels/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var channel = await db.Channels
                .FirstOrDefaultAsync(m => m.Id == id);
            if (channel == null)
            {
                return NotFound();
            }

            return View(channel);
        }

        // GET: Channels/Create
        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> Create(ulong id)
        {
            var discord = await discordHelper.LoginBot();

            var dbChannels = await db.Channels.ToListAsync();
            var availableChannels = discord.Guilds
                .SelectMany(g => g.TextChannels.Where(dc => dbChannels.All(c => c.DiscordId != dc.Id)))
                .OrderBy(c => c.Guild.Name)
                .ThenBy(c => c.Name)
                .Select(c => new SelectListItem(
                    $"{c.Guild.Name}: {c.Name}",
                    $"{c.Id}",
                    c.Id == id));

            ViewBag.Channels = availableChannels;
            return View();
        }

        // POST: Channels/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> Create([Bind("DiscordId,IsMonitored")] Channel channel)
        {
            if (ModelState.IsValid)
            {
                db.Add(channel);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(channel);
        }

        // GET: Channels/Edit/5
        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var channel = await db.Channels.FindAsync(id);
            if (channel == null)
            {
                return NotFound();
            }

            return View(channel);
        }

        // POST: Channels/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DiscordId,IsMonitored")] Channel channel)
        {
            if (id != channel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Update(channel);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChannelExists(channel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(channel);
        }

        // GET: Channels/Delete/5
        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var channel = await db.Channels
                .FirstOrDefaultAsync(m => m.Id == id);
            if (channel == null)
            {
                return NotFound();
            }

            return View(channel);
        }

        // POST: Channels/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var channel = await db.Channels.FindAsync(id);
            db.Channels.Remove(channel);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> Message(int id)
        {
            var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Id == id);
            var discord = await discordHelper.LoginBot();

            var proxies = await db.ProxyChannels
                .Include(pc => pc.Channel)
                .Include(pc => pc.Proxy)
                .Where(pc => pc.Channel.Id == dbChannel.Id || pc.Proxy.IsGlobal)
                .Select(pc => pc.Proxy)
                .ToListAsync();

            var l = new List<SelectListItem>
            {
                new SelectListItem("Bot", $"{MESSAGE_FOR_BOT}"),
                new SelectListItem("----", "", false, true)
            };
            l.AddRange(proxies.Select(p => new SelectListItem(p.Name, $"{p.Id}", false)));

            ViewBag.Proxies = l;

            var discordChannel = discord.GetChannel(dbChannel.DiscordId) as ITextChannel;
            return View(new SendChannelMessageViewModel
            {
                ChannelDiscordId = dbChannel.DiscordId,
                ChannelDatabaseId = dbChannel.Id,
                ChannelName = discordChannel?.Name,
                GuildName = discordChannel?.Guild.Name,
            });
        }

        [HttpPost]
        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> Message(SendChannelMessageViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var discord = await discordHelper.LoginBot();
                if (viewModel.ProxyId == MESSAGE_FOR_BOT)
                {
                    if (discord.GetChannel(viewModel.ChannelDiscordId) is ITextChannel tc)
                    {
                        await tc.SendMessageAsync(viewModel.Message);
                    }
                }
                else
                {
                    var ph = new ProxyHelper(discord, cfg);
                    var proxy = await db.Proxies.FirstOrDefaultAsync(p => p.Id == viewModel.ProxyId);
                    if (discord.GetChannel(viewModel.ChannelDiscordId) is ITextChannel tc)
                    {
                        var webhook = await webhookCache.GetWebhook(tc, discord);
                        await ph.SendMessage(webhook, proxy, viewModel.Message, null);
                    }
                }
            }

            return RedirectToAction(nameof(Message), new {id = viewModel.ChannelDatabaseId});
        }

        [Authorize(Policy = DbRoleRequirement.RequiresAdmin)]
        public async Task<IActionResult> Messages(int id, ulong? fromMessageId)
        {
            var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Id == id);
            var discord = await discordHelper.LoginBot();
            if (discord.GetChannel(dbChannel.DiscordId) is SocketTextChannel tc)
            {
                var messageChunks = fromMessageId is ulong msgId
                    ? tc.GetMessagesAsync(msgId, Direction.After)
                    : tc.GetMessagesAsync();
                var messages = await messageChunks.Flatten().ToListAsync();
                return Json(messages.Select(m => new
                {
                    messageId = m.Id,
                    authorUsername = m.Author.Username,
                    avatarUrl = m.Author.GetAvatarUrl(),
                    content = m.Content
                }));
            }

            return NotFound();
        }

        private bool ChannelExists(int id)
        {
            return db.Channels.Any(e => e.Id == id);
        }
    }
}
