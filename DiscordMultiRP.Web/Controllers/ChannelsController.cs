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

namespace DiscordMultiRP.Web.Controllers
{
    [Authorize]
    public class ChannelsController : Controller
    {
        private readonly ProxyDataContext db;
        private readonly DiscordHelper discordHelper;

        public ChannelsController(ProxyDataContext db, DiscordHelper discordHelper)
        {
            this.db = db;
            this.discordHelper = discordHelper;
        }

        // GET: Channels
        public async Task<IActionResult> Index()
        {
            var discord = await discordHelper.LoginBot();
            if (discord == null)
            {
                return NotFound("Can't connect to Discord.");
            }

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
        public async Task<IActionResult> Create(ulong id)
        {
            var discord = await discordHelper.LoginBot();
            if (discord == null)
            {
                return NotFound("Can't connect to Discord.");
            }

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
        public async Task<IActionResult> Create([Bind("Id,DiscordId,IsMonitored")] Channel channel)
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var channel = await db.Channels.FindAsync(id);
            db.Channels.Remove(channel);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChannelExists(int id)
        {
            return db.Channels.Any(e => e.Id == id);
        }
    }
}
