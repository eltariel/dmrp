using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DiscordMultiRP.Bot.Data;
using Microsoft.AspNetCore.Authorization;

namespace DiscordMultiRP.Web.Controllers
{
    [Authorize]
    public class ProxiesController : Controller
    {
        private readonly ProxyDataContext db;

        public ProxiesController(ProxyDataContext db)
        {
            this.db = db;
        }

        // GET: Proxies
        public async Task<IActionResult> Index()
        {
            var id = GetDiscordId();
            var user = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == id);
            ViewBag.User = user;

            var contextProxies = user.Role == Role.Admin
                ? db.Proxies.Include(p => p.User)
                : db.Proxies.Where(p => p.User.DiscordId == id);

            return View(await contextProxies.ToListAsync());
        }

        private ulong GetDiscordId()
        {
            return ulong.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        // GET: Proxies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

        // GET: Proxies/Create
        public async Task<IActionResult> Create()
        {
            await GetDiscordDbUser();

            return View();
        }

        private async Task<User> GetDiscordDbUser()
        {
            var discordId = GetDiscordId();
            var user = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);
            ViewBag.User = user;
            return user;
        }

        // POST: Proxies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Prefix,Suffix,IsReset,IsGlobal")] Proxy proxy)
        {
            if (ModelState.IsValid)
            {
                var user = await GetDiscordDbUser();
                if (user == null || user.Role == Role.None)
                {
                    return Forbid();
                }

                if (!user.IsAllowedGlobal)
                {
                    proxy.IsGlobal = false;
                }

                proxy.User = user;

                db.Add(proxy);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(proxy);
        }

        // GET: Proxies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proxy = await db.Proxies.FindAsync(id);
            if (proxy == null)
            {
                return NotFound();
            }

            await GetDiscordDbUser();
            return View(proxy);
        }

        // POST: Proxies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Prefix,Suffix,IsReset,IsGlobal")] Proxy proxy)
        {
            if (id != proxy.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await GetDiscordDbUser();
                    if (user != null)
                    {
                        if (!user.IsAllowedGlobal)
                        {
                            proxy.IsGlobal = false;
                        }

                        db.Update(proxy);
                        await db.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProxyExists(proxy.Id))
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
            return View(proxy);
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
            return RedirectToAction(nameof(Index));
        }

        private bool ProxyExists(int id)
        {
            return db.Proxies.Any(e => e.Id == id);
        }
    }
}
