using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DiscordMultiRP.Web.Controllers
{
    public class AvatarController : Controller
    {
        private readonly ProxyDataContext db;
        private readonly IConfiguration cfg;
        private readonly string avatarPath;

        public AvatarController(ProxyDataContext db, IConfiguration cfg)
        {
            this.db = db;
            this.cfg = cfg;

            var cfgAvatar = cfg["Discord:avatar-path"];
            avatarPath = !string.IsNullOrWhiteSpace(cfgAvatar)
                ? cfgAvatar
                : Path.Combine(Environment.GetEnvironmentVariable("home"), "dmrp", "avatars");
        }

        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> View(Guid id)
        {
            if (id == Guid.Empty)
            {
                return NotFound();
            }

            var proxy = await db.Proxies.FirstOrDefaultAsync(p => p.AvatarGuid == id);
            if (string.IsNullOrWhiteSpace(proxy?.AvatarContentType))
            {
                return NotFound();
            }

            var avatar = Directory.EnumerateFiles(avatarPath, $"{proxy.Id}.*").FirstOrDefault();
            if (avatar != null)
            {
                return PhysicalFile(avatar, proxy.AvatarContentType);
            }

            return NotFound();
        }
    }
}