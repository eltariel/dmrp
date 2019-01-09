using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DiscordMultiRP.Bot.Data;
using DiscordMultiRP.Web.Models;
using DiscordMultiRP.Web.Util;
using Microsoft.Extensions.Configuration;

namespace DiscordMultiRP.Web.Controllers
{
    public class UsersController : Controller
    {
        private readonly ProxyDataContext _context;
        private readonly IConfiguration cfg;
        private readonly DiscordHelper discordHelper;

        public UsersController(ProxyDataContext context, IConfiguration cfg, DiscordHelper discordHelper)
        {
            _context = context;
            this.cfg = cfg;
            this.discordHelper = discordHelper;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var discord = await discordHelper.LoginBot();
            var dUsers = discord.Guilds.SelectMany(g => g.Users).Distinct().ToList();
            var dbUsers = await _context.Users.ToListAsync();
            var modelUsers = dbUsers.Select(u => new UserViewModel(u, dUsers.FirstOrDefault(d => d.Id == u.DiscordId)));

            return View(modelUsers);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            var discord = await discordHelper.LoginBot();
            var dUsers = discord.Guilds.SelectMany(g => g.Users).Distinct().ToList();
            var dbUsers = await _context.Users.ToListAsync();
            var modelUsers = dUsers
                .Where(d => dbUsers.All(u => u.DiscordId != d.Id))
                .Select(d => new SelectListItem($"{d.Username}#{d.Discriminator}", $"{d.Id}"))
                .Distinct()
                .ToList();

            ViewBag.Users = modelUsers;
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DiscordId,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DiscordId,Role")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

    }
}
