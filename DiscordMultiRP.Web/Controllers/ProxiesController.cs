using DiscordMultiRP.Bot.Data;
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
using System.Net.Mime;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using static MoreLinq.Extensions.DistinctByExtension;

namespace DiscordMultiRP.Web.Controllers
{
    [Authorize]
    public class ProxiesController : Controller
    {
        private readonly ProxyDataContext db;
        private readonly IConfiguration cfg;
        private readonly DiscordHelper discordHelper;
        private readonly string avatarPath = Path.Combine(Environment.GetEnvironmentVariable("home"), "dmrp", "avatars");

        public ProxiesController(ProxyDataContext db, IConfiguration cfg, DiscordHelper discordHelper)
        {
            this.db = db;
            this.cfg = cfg;
            this.discordHelper = discordHelper;
        }

        private ulong DiscordUserId => DiscordHelper.GetDiscordUserIdFor(User);

        // GET: Proxies
        public async Task<IActionResult> Index()
        {
            var id = DiscordUserId;
            var user = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == id);
            ViewBag.User = user;

            var contextProxies = await (user.IsAdmin
                ? db.Proxies.Include(p => p.User)
                : db.Proxies.Where(p => p.User.DiscordId == id)).ToListAsync();

            IEnumerable<ProxyViewModel> pvms;
            if (user.IsAdmin)
            {
                var discord = await discordHelper.LoginBot();
                if (discord == null)
                {
                    ViewBag.DiscordUnavailable = true;
                }

                pvms = contextProxies.Select(p =>
                {
                    var username = discord?.GetUser(p.User.DiscordId) is IUser du
                        ? $"{du.Username}#{du.Discriminator}"
                        : $"Discord UserId: {p.User.DiscordId}";

                    return new ProxyViewModel(p, username);
                });
            }
            else
            {
                pvms = contextProxies.Select(p => new ProxyViewModel(p));
            }

            return View(pvms.ToList());
        }

        // GET: Proxies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dbUser = await GetDbUser();
            ViewBag.User = dbUser;

            var proxy = await db.Proxies
                .Include(p => p.User)
                .Include(p => p.Channels).ThenInclude(pc => pc.Channel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (proxy == null)
            {
                return NotFound();
            }

            return View(proxy);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Avatar(int id)
        {
            var proxy = await db.Proxies.FirstOrDefaultAsync(p => p.Id == id);
            if (string.IsNullOrWhiteSpace(proxy?.AvatarContentType))
            {
                return NotFound();
            }

            var avatar = Directory.EnumerateFiles(avatarPath, $"{id}.*").FirstOrDefault();
            if (avatar != null)
            {
                return PhysicalFile(avatar, proxy.AvatarContentType);
            }

            return NotFound();
        }

        // GET: Proxies/Create
        public async Task<IActionResult> Create()
        {
            var dbUser = await GetDbUser();
            ViewBag.User = dbUser;
            var discord = await discordHelper.LoginBot();
            if (discord == null)
            {
                ViewBag.DiscordUnavailable = true;
            }

            var user = discord.GetUser(DiscordUserId);
            var myChannels = user
                .MutualGuilds
                .SelectMany(g => g.TextChannels)
                .ToList();

            if (dbUser.IsAdmin)
            {
                var otherUsers = user
                    .MutualGuilds
                    .SelectMany(g => g.Users)
                    .DistinctBy(u => u.Id)
                    .ToList();

                var visibleUsers = otherUsers.OrderBy(u => u.Username)
                    .Prepend(user)
                    .Select(u => new SelectListItem(
                        $"{u.Username}#{u.DiscriminatorValue}",
                        $"{u.Id}",
                        u.Id == user.Id));

                var channelsByUser = otherUsers
                    .ToDictionary(
                        u => u.Id,
                        u => u.MutualGuilds
                            .SelectMany(g => g.TextChannels)
                            .Intersect(myChannels)
                            .Select(c => new SelectListItem($"{c.Guild.Name}: {c.Name}", $"{c.Id}"))
                            .ToList());

                ViewBag.VisibleUsers = visibleUsers;
                ViewBag.ChannelsByUser = channelsByUser;
            }

            ViewBag.Channels = myChannels.Select(c => new SelectListItem($"{c.Guild.Name}: {c.Name}", $"{c.Id}")); ;

            return View();
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
                var user = await GetDbUser();
                if (user == null || user.Role == Role.None)
                {
                    return Forbid();
                }

                if (!user.IsAdmin && pvm.UserDiscordId != user.DiscordId)
                {
                    return Forbid();
                }

                if (!user.IsAllowedGlobal)
                {
                    pvm.IsGlobal = false;
                }

                var proxyUser = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == pvm.UserDiscordId) ??
                                new User { DiscordId = pvm.UserDiscordId };

                var proxy = new Proxy
                {
                    Name = pvm.Name,
                    AvatarContentType = pvm.Avatar?.ContentType,
                    Prefix = pvm.Prefix,
                    Suffix = pvm.Suffix,
                    IsGlobal = pvm.IsGlobal,
                    IsReset = pvm.IsReset,
                    User = proxyUser,
                };

                await UpdateProxyChannels(proxy, pvm);
                await UpdateAvatar(pvm, proxy);

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
                .Include(p => p.User)
                .Include(p => p.Channels).ThenInclude(c => c.Channel)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (proxy == null)
            {
                return NotFound();
            }

            ViewBag.User = await GetDbUser();

            var discord = await discordHelper.LoginBot();
            if (discord == null)
            {
                return NotFound("Can't connect to Discord.");
            }

            var userChannels = discord.GetUser(DiscordUserId)
                .MutualGuilds
                .SelectMany(g => g.TextChannels)
                .Select(c => new SelectListItem($"{c.Guild.Name}: {c.Name}", $"{c.Id}"));
            ViewBag.Channels = userChannels;

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
                    var user = await GetDbUser();
                    if (user != null)
                    {
                        if (!user.IsAllowedGlobal)
                        {
                            pvm.IsGlobal = false;
                        }

                        var proxy = await db.Proxies
                            .Include(p => p.User)
                            .Include(p => p.Channels).ThenInclude(c => c.Channel)
                            .FirstOrDefaultAsync(p => p.Id == id);
                        if (proxy == null)
                        {
                            return NotFound();
                        }

                        proxy.IsGlobal = pvm.IsGlobal;
                        proxy.IsReset = pvm.IsReset;
                        proxy.Name = pvm.Name;
                        proxy.Prefix = pvm.Prefix;
                        proxy.Suffix = pvm.Suffix;

                        await UpdateProxyChannels(proxy, pvm);
                        await UpdateAvatar(pvm, proxy);

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
            if (id == null)
            {
                return NotFound();
            }

            var proxy = await db.Proxies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (proxy == null)
            {
                return NotFound();
            }

            return View(proxy);
        }

        // POST: Proxies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proxy = await db.Proxies.FindAsync(id);
            db.Proxies.Remove(proxy);
            await db.SaveChangesAsync();
            DeleteExistingAvatars(id);
            return RedirectToAction(nameof(Index));
        }

        private bool ProxyExists(int id)
        {
            return db.Proxies.Any(e => e.Id == id);
        }

        private async Task UpdateProxyChannels(Proxy proxy, ProxyViewModel pvm)
        {
            if (!pvm.IsGlobal)
            {
                if (pvm.Channels?.Any() ?? false)
                {
                    var dbChannels = await db.Channels
                        .Where(c => pvm.Channels.Contains(c.DiscordId))
                        .DefaultIfEmpty()
                        .ToListAsync();

                    proxy.Channels = pvm.Channels
                        .Select(id => new ProxyChannel
                        {
                            Channel = dbChannels.FirstOrDefault(dc => dc.DiscordId == id) ??
                                      new Channel { DiscordId = id, IsMonitored = true },
                            Proxy = proxy
                        })
                        .ToList();
                }
            }
            else
            {
                proxy.Channels.Clear();
            }
        }

        private async Task<User> GetDbUser()
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == DiscordUserId);
            ViewBag.User = user;
            return user;
        }

        private async Task UpdateAvatar(ProxyViewModel pvm, Proxy proxy)
        {
            if ((pvm.Avatar?.Length ?? 0) > 0)
            {
                Directory.CreateDirectory(avatarPath);
                DeleteExistingAvatars(pvm.Id);
                proxy.AvatarContentType = pvm.Avatar.ContentType;

                var ext = Path.GetExtension(pvm.Avatar.FileName);
                var avatar = Path.Combine(avatarPath, $"{pvm.Id}.{ext}");
                using (var s = System.IO.File.Open(avatar, FileMode.Create))
                {
                    await pvm.Avatar.CopyToAsync(s);
                }
            }
        }

        private void DeleteExistingAvatars(int id)
        {
            foreach (var file in Directory.EnumerateFiles(avatarPath, $"{id}.*"))
            {
                System.IO.File.Delete(Path.Combine(avatarPath, file));
            }
        }
    }
}