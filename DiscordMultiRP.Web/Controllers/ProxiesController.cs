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
            var contextProxies = db.Proxies.Where(p => p.User.DiscordId == id);
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
                .Include(p => p.Channel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (proxy == null)
            {
                return NotFound();
            }

            return View(proxy);
        }

        // GET: Proxies/Create
        public IActionResult Create()
        {
            
            return View();
        }

        // POST: Proxies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Prefix,Suffix,IsGlobal")] Proxy proxy)
        {
            if (ModelState.IsValid)
            {
                var discordId = GetDiscordId();
                var user = await db.Users.FirstOrDefaultAsync(u=>u.DiscordId == discordId);
                if (user == null)
                {
                    user = new User{DiscordId = discordId};
                    db.Add(user);
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
            return View(proxy);
        }

        // POST: Proxies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Prefix,Suffix,IsGlobal")] Proxy proxy)
        {
            if (id != proxy.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Update(proxy);
                    await db.SaveChangesAsync();
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
