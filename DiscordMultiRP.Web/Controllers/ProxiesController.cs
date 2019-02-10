using DiscordMultiRP.Web.Models;
using DiscordMultiRP.Web.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordMultiRP.Bot.Data;
using Microsoft.Extensions.Logging;
using static MoreLinq.Extensions.DistinctByExtension;

namespace DiscordMultiRP.Web.Controllers
{
    [Authorize]
    [RequireDiscord]
    public class ProxiesController : Controller
    {
        private readonly ProxyDataContext db;
        private readonly IConfiguration cfg;
        private readonly DiscordHelper discordHelper;
        private readonly AvatarHelper avatarHelper;
        private readonly ILogger<ProxiesController> logger;

        public ProxiesController(ProxyDataContext db, IConfiguration cfg, DiscordHelper discordHelper,
            AvatarHelper avatarHelper, ILogger<ProxiesController> logger)
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
            var pd = db.ProxyDetails;
            var proxies = await (CurrentUser.IsAdmin
                    ? pd.Where(p => !id.HasValue || p.BotUser.Id == id)
                    : pd.Where(p => p.BotUser.DiscordId == DiscordUserId))
                .ToListAsync();

            var discord = await discordHelper.LoginBot();
            var pvms = proxies.Select(p => new ProxyViewModel(p, discord));

            return View(pvms.OrderBy(p => p.BotUserId != CurrentUser.Id)
                .ThenBy(p => p.UserName)
                .ThenBy(p => p.Name)
                .ToList());
        }

        // GET: Proxies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var discord = await discordHelper.LoginBot();

            if (id == null)
            {
                return NotFound();
            }

            var proxy = await db.FindProxyByIdAsync(id);
            if (proxy == null)
            {
                return NotFound();
            }

            var pvm = new ProxyViewModel(proxy, discord);
            return View(pvm);
        }

        // GET: Proxies/Create
        public async Task<IActionResult> Create()
        {
            var discord = await discordHelper.LoginBot();
            await PopulateCreateView(discord);

            return View();
        }

        // POST: Proxies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProxyCreateViewModel pcvm)
        {
            if (ModelState.IsValid)
            {
                var proxyUser = await db.BotUsers.FirstOrDefaultAsync(u => u.DiscordId == pcvm.DiscordUserId) ??
                                new BotUser {DiscordId = pcvm.DiscordUserId};

                var proxy = new Proxy
                {
                    Name = pcvm.Name,
                    Prefix = pcvm.Prefix,
                    Suffix = pcvm.Suffix,
                    Biography = pcvm.Biography,
                    IsGlobal = proxyUser.IsAllowedGlobal && pcvm.IsGlobal,
                    BotUser = proxyUser,
                };

                if (!CurrentUser.CanEditFor(proxyUser) || !await UpdateProxyChannels(proxy, pcvm.Channels, pcvm.IsGlobal))
                {
                    return Forbid();
                }

                await avatarHelper.UpdateAvatar(proxy, pcvm.Avatar);

                db.Add(proxy);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var discord = await discordHelper.LoginBot();
            await PopulateCreateView(discord);
            return View(pcvm);
        }

        // GET: Proxies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proxy = await db.FindProxyByIdAsync(id);
            if (proxy == null)
            {
                return NotFound();
            }

            if (!CurrentUser.CanEdit(proxy))
            {
                return Forbid();
            }

            var discord = await discordHelper.LoginBot();
            ViewBag.Channels = await GetUserAllowedChannels(
                discord.GetUser(DiscordUserId),
                discord.GetUser(proxy.BotUser.DiscordId));

            return View(new ProxyViewModel(proxy, discord));
        }

        // POST: Proxies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,  ProxyEditViewModel pevm)
        {
            if (id != pevm.Id)
            {
                return NotFound();
            }

            var proxy = await db.FindProxyByIdAsync(id);
            if (proxy == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (!CurrentUser.CanEdit(proxy))
                {
                    return Forbid();
                }

                proxy.IsGlobal = CurrentUser.IsAllowedGlobal && pevm.IsGlobal;
                proxy.Name = pevm.Name;
                proxy.Prefix = pevm.Prefix;
                proxy.Suffix = pevm.Suffix;
                proxy.Biography = pevm.Biography;

                if (!await UpdateProxyChannels(proxy, pevm.Channels, pevm.IsGlobal))
                {
                    return Forbid();// TODO: Make this fail gracefully rather than returning HTTP Fuck off
                }

                await avatarHelper.UpdateAvatar(proxy, pevm.Avatar);

                db.Update(proxy);
                await db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            var discord = await discordHelper.LoginBot();
            var pvm = new ProxyViewModel(proxy, discord)
            {
                IsGlobal = CurrentUser.IsAllowedGlobal && pevm.IsGlobal,
                Name = pevm.Name,
                Prefix = pevm.Prefix,
                Suffix = pevm.Suffix,
                Biography = pevm.Biography,
                Avatar = pevm.Avatar,
                Channels = pevm.Channels,
            };
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
            var proxy = await db.FindProxyByIdAsync(id);
            if (proxy == null)
            {
                return NotFound();
            }

            if(!CurrentUser.CanEdit(proxy))
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

            var proxy = await db.FindProxyByIdAsync(id);
            if (proxy == null)
            {
                return NotFound();
            }

            if(!CurrentUser.CanEdit(proxy))
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

            var proxy = await db.FindProxyByIdAsync(id);
            if (proxy != null && CurrentUser.CanEdit(proxy))
            {
                await avatarHelper.DeleteAvatarFor(proxy);
            }

            return RedirectToAction(nameof(Edit), new {id});
        }

        private async Task<bool> UpdateProxyChannels(Proxy proxy, List<ulong> channelIds, bool isGlobal)
        {
            if (!isGlobal)
            {
                if (channelIds?.Any() ?? false)
                {
                    var dbChannels = await db.Channels
                        .Where(c => channelIds.Contains(c.DiscordId))
                        .ToListAsync();

                    proxy.Channels = channelIds
                        .DefaultIfEmpty()
                        .Select(id => new ProxyChannel
                        {
                            Channel = dbChannels.FirstOrDefault(dc => dc.DiscordId == id) ??
                                      new Channel {DiscordId = id, IsMonitored = proxy.BotUser.IsAdmin},
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
                        !(dbChannel.IsMonitored || CurrentUser.IsAllowedGlobal)))
                .ToList();
            return allowedChannels;
        }

        private async Task PopulateCreateView(DiscordSocketClient discord)
        {
            var discordUser = discord.GetUser(DiscordUserId);
            var allowedChannels = await GetUserAllowedChannels(discordUser, discordUser);

            ViewBag.DiscordUserId = DiscordUserId;

            if (CurrentUser.IsAdmin)
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
            }

            ViewBag.Channels = allowedChannels;
        }


        private BotUser CurrentUser => (BotUser)HttpContext.Items[typeof(BotUser)];
    }
}