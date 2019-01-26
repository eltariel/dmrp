using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;
using DiscordMultiRP.Web.Util;
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
        private readonly AvatarHelper avatarHelper;

        public AvatarController(ProxyDataContext db, IConfiguration cfg, AvatarHelper avatarHelper)
        {
            this.db = db;
            this.cfg = cfg;
            this.avatarHelper = avatarHelper;
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
            if (!proxy.HasAvatar)
            {
                return NotFound();
            }

            var avatar = avatarHelper.PathFor(proxy);
            if (avatar != null)
            {
                return PhysicalFile(avatar, proxy.AvatarContentType);
            }

            return NotFound();
        }
    }
}