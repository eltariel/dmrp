﻿using DiscordMultiRP.Bot.Data;
using DiscordMultiRP.Web.Models;
using DiscordMultiRP.Web.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static MoreLinq.Extensions.DistinctByExtension;

namespace DiscordMultiRP.Web.Controllers
{
    [Authorize]
    public class ProxiesController : Controller
    {
        private readonly ProxyDataContext db;
        private readonly IConfiguration cfg;
        private readonly DiscordHelper discordHelper;
        private readonly AvatarHelper avatarHelper;
        private readonly ILogger<ProxiesController> logger;
        private DiscordSocketClient discord;
        private BotUser botUser;

        public ProxiesController(ProxyDataContext db, IConfiguration cfg, DiscordHelper discordHelper, AvatarHelper avatarHelper, ILogger<ProxiesController> logger)
        {
            this.db = db;
            this.cfg = cfg;
            this.discordHelper = discordHelper;
            this.avatarHelper = avatarHelper;
            this.logger = logger;
        }

        private ulong DiscordUserId => DiscordHelper.GetDiscordUserIdFor(User);

        // GET: Proxies
        public async Task<IActionResult> Index(int? id)
        {
            var contextProxies = await (botUser.IsAdmin
                    ? db.Proxies.Include(p => p.BotUser).Where(p => !id.HasValue || p.BotUser.Id == id)
                    : db.Proxies.Where(p => p.BotUser.DiscordId == DiscordUserId))
                .ToListAsync();

            IEnumerable<ProxyViewModel> pvms;
            if (botUser.IsAdmin)
            {
                pvms = contextProxies.Select(p =>
                {
                    var username = discord?.GetUser(p.BotUser.DiscordId) is IUser du
                        ? $"{du.Username}#{du.Discriminator}"
                        : $"Discord UserId: {p.BotUser.DiscordId}";

                    return new ProxyViewModel(p, username);
                });
            }
            else
            {
                pvms = contextProxies.Select(p => new ProxyViewModel(p));
            }

            return View(pvms.OrderBy(p => p.UserId != botUser.Id)
                .ThenBy(p => p.UserName)
                .ThenBy(p => p.Name)
                .ToList());
        }

        // GET: Proxies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proxy = await db.Proxies
                .Include(p => p.BotUser)
                .Include(p => p.Channels).ThenInclude(pc => pc.Channel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (proxy == null)
            {
                return NotFound();
            }

            var discordUser = discord.GetUser(DiscordUserId);
            var channels = discordUser
                .MutualGuilds
                .SelectMany(g => g.TextChannels)
                .Where(c => proxy.Channels.Select(pc => pc.Channel.DiscordId).Contains(c.Id))
                .ToList();
            var pvm = new ProxyViewModel(proxy, $"{discordUser.Username}#{discordUser.Discriminator}", channels);

            return View(pvm);
        }

        // GET: Proxies/Create
        public async Task<IActionResult> Create()
        {
            var discordUser = discord.GetUser(DiscordUserId);
            var allowedChannels = await GetUserAllowedChannels(discordUser, discordUser);

            ViewBag.DiscordUserId = DiscordUserId;

            if (botUser.IsAdmin)
            {
                var otherUsers = discordUser
                    .MutualGuilds
                    .SelectMany(g => g.Users)
                    .DistinctBy(u => u.Id)
                    .ToList();

                var visibleUsers = otherUsers.OrderBy(u => u.Username)
                    .Prepend(discordUser)
                    .Select(u => new SelectListItem(
                        $"{u.Username}#{u.Discriminator}",
                        $"{u.Id}",
                        u.Id == discordUser.Id));

                ViewBag.VisibleUsers = visibleUsers;

                //var channelsByUser = otherUsers
                //    .ToDictionary(
                //        u => u.Id,
                //        u => u.MutualGuilds
                //            .SelectMany(g => g.TextChannels)
                //            .Intersect(myChannels)
                //            .Select(c => new SelectListItem(
                //                $"{c.Guild.Name}: {c.Name}",
                //                $"{c.Id}",
                //                false,
                //                false))
                //            .ToList());
                //ViewBag.ChannelsByUser = channelsByUser;
            }

            ViewBag.Channels = allowedChannels;

            return View();
        }

        private async Task<List<SelectListItem>> GetUserAllowedChannels(SocketUser self, SocketUser otherUser)
        {
            var myChannels = self
                .MutualGuilds
                .SelectMany(g => g.TextChannels);
            var theirChannels = otherUser
                .MutualGuilds
                .SelectMany(g => g.TextChannels);
            var visibleChannels = myChannels.Intersect(theirChannels).ToList();
            var dbChannels = await db.Channels
                .Where(c => visibleChannels.Exists(d => d.Id == c.DiscordId))
                .ToListAsync();
            var allowedChannels = visibleChannels
                .Join(dbChannels,
                    c => c.Id,
                    c => c.DiscordId,
                    (discordChannel, dbChannel) => new SelectListItem(
                        $"{discordChannel.Guild.Name}: {discordChannel.Name}",
                        $"{discordChannel.Id}",
                        false,
                        !(dbChannel.IsMonitored || botUser.IsAllowedGlobal)))
                .ToList();
            return allowedChannels;
        }

        // POST: Proxies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserDiscordId,Name,Prefix,Suffix,IsReset,IsGlobal,Channels,Avatar")] ProxyViewModel pvm)
        {
            if (ModelState.IsValid)
            {
                if (botUser == null || botUser.Role == Role.None)
                {
                    return Forbid();
                }

                if (!botUser.IsAdmin && pvm.UserDiscordId != botUser.DiscordId)
                {
                    return Forbid();
                }

                if (!botUser.IsAllowedGlobal)
                {
                    pvm.IsGlobal = false;
                }

                var proxyUser = await db.BotUsers.FirstOrDefaultAsync(u => u.DiscordId == pvm.UserDiscordId) ??
                                new BotUser {DiscordId = pvm.UserDiscordId};

                var proxy = new Proxy
                {
                    Name = pvm.Name,
                    Prefix = pvm.Prefix,
                    Suffix = pvm.Suffix,
                    IsGlobal = pvm.IsGlobal,
                    BotUser = proxyUser,
                };

                if (!await UpdateProxyChannels(proxy, pvm))
                {
                    return Forbid();
                }

                await avatarHelper.UpdateAvatar(proxy, pvm.Avatar);

                db.Add(proxy);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(pvm);
        }

        // GET: Proxies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proxy = await db.Proxies
                .Include(p => p.BotUser)
                .Include(p => p.Channels).ThenInclude(c => c.Channel)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (proxy == null)
            {
                return NotFound();
            }

            if (discord == null)
            {
                return NotFound("Can't connect to Discord.");
            }

            ViewBag.Channels = await GetUserAllowedChannels(
                discord.GetUser(DiscordUserId),
                discord.GetUser(proxy.BotUser.DiscordId));

            return View(new ProxyViewModel(proxy));
        }

        // POST: Proxies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Prefix,Suffix,IsReset,IsGlobal,Channels,Avatar")] ProxyViewModel pvm)
        {
            if (id != pvm.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (botUser != null)
                    {
                        if (!botUser.IsAllowedGlobal)
                        {
                            pvm.IsGlobal = false;
                        }

                        var proxy = await db.Proxies
                            .Include(p => p.BotUser)
                            .Include(p => p.Channels).ThenInclude(c => c.Channel)
                            .FirstOrDefaultAsync(p => p.Id == id);
                        if (proxy == null)
                        {
                            return NotFound();
                        }

                        proxy.IsGlobal = pvm.IsGlobal;
                        proxy.Name = pvm.Name;
                        proxy.Prefix = pvm.Prefix;
                        proxy.Suffix = pvm.Suffix;

                        if (!await UpdateProxyChannels(proxy, pvm))
                        {
                            return Forbid();
                        }

                        await avatarHelper.UpdateAvatar(proxy, pvm.Avatar);

                        db.Update(proxy);
                        await db.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProxyExists(pvm.Id))
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

            return View(pvm);
        }

        // GET: Proxies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if ((id ?? 0) == 0)
            {
                return NotFound();
            }

            logger.LogDebug($"Delete request for user ID {id}");
            var proxy = await db.Proxies
                .Include(p => p.BotUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (proxy == null)
            {
                return NotFound();
            }

            if (botUser.Id != proxy.BotUser.Id && !botUser.IsAdmin)
            {
                return Forbid();
            }

            return View(proxy);
        }

        // POST: Proxies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var proxy = await db.Proxies
                .Include(p => p.BotUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (proxy == null)
            {
                return NotFound();
            }

            if (botUser.Id != proxy.BotUser.Id && !botUser.IsAdmin)
            {
                return Forbid();
            }

            db.Proxies.Remove(proxy);
            await db.SaveChangesAsync();
            avatarHelper.DeleteExistingAvatars(proxy.AvatarGuid);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemoveAvatar(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var proxy = await db.Proxies.FirstOrDefaultAsync(p => p.Id == id);
            if (proxy != null)
            {
                await avatarHelper.DeleteAvatarFor(proxy);
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                (discord, botUser) = await CommonSetup();
                var resultContext = await next();
            }
            catch (UnauthorizedAccessException uaex)
            {
                context.Result = Unauthorized(uaex);
            }
        }

        private async Task<(DiscordSocketClient discord, BotUser botUser)> CommonSetup()
        {
            var discord = await discordHelper.LoginBot();
            if (discord == null)
            {
                ViewBag.DiscordUnavailable = true;
            }

            var botUser = await GetBotUser();
            if (botUser == null)
            {
                if (discord?.GetUser(DiscordUserId)?.MutualGuilds.Any() ?? false)
                {
                    botUser = new BotUser {DiscordId = DiscordUserId, Role = Role.User};
                    db.BotUsers.Add(botUser);
                    await db.SaveChangesAsync();
                }
                else
                {
                    throw new UnauthorizedAccessException($"Unknown user {DiscordUserId}.");
                }
            }

            ViewBag.User = botUser;

            return (discord, botUser);
        }

        private bool ProxyExists(int id)
        {
            return db.Proxies.Any(e => e.Id == id);
        }

        private async Task<bool> UpdateProxyChannels(Proxy proxy, ProxyViewModel pvm)
        {
            if (!pvm.IsGlobal)
            {
                if (pvm.Channels?.Any() ?? false)
                {
                    var dbChannels = await db.Channels
                        .Where(c => pvm.Channels.Contains(c.DiscordId))
                        .ToListAsync();

                    proxy.Channels = pvm.Channels
                        .DefaultIfEmpty()
                        .Select(id => new ProxyChannel
                        {
                            Channel = dbChannels.FirstOrDefault(dc => dc.DiscordId == id) ??
                                      new Channel { DiscordId = id, IsMonitored = proxy.BotUser.IsAdmin },
                            Proxy = proxy
                        })
                        .ToList();

                    if (!proxy.BotUser.IsAdmin && proxy.Channels.Select(c => c.Channel).Any(c => !c.IsMonitored))
                    {
                        return false;
                    }
                }
            }
            else
            {
                proxy.Channels?.Clear();
            }

            return true;
        }

        private async Task<BotUser> GetBotUser()
        {
            var user = await db.BotUsers.FirstOrDefaultAsync(u => u.DiscordId == DiscordUserId);
            ViewBag.User = user;
            return user;
        }
    }
}