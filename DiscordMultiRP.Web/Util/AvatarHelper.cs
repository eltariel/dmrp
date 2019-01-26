using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DiscordMultiRP.Web.Util
{
    public class AvatarHelper
    {
        private readonly ProxyDataContext db;
        private readonly IConfiguration cfg;

        public AvatarHelper(ProxyDataContext db, IConfiguration cfg)
        {
            this.db = db;
            this.cfg = cfg;

            AvatarPath = !string.IsNullOrWhiteSpace(cfg["Discord:avatar-path"])
                ? cfg["Discord:avatar-path"]
                : Path.Combine(Environment.GetEnvironmentVariable("home"), "dmrp", "avatars");
        }

        public string AvatarPath { get; }

        public void DeleteExistingAvatars(Guid id)
        {
            foreach (var file in Directory.EnumerateFiles(AvatarPath, $"{id}.*"))
            {
                File.Delete(Path.Combine(AvatarPath, file));
            }
        }

        public async Task UpdateAvatar(Proxy proxy, IFormFile avatar)
        {
            if ((avatar?.Length ?? 0) > 0)
            {
                Directory.CreateDirectory(AvatarPath);
                DeleteExistingAvatars(proxy.AvatarGuid);
                proxy.AvatarGuid = Guid.NewGuid();
                proxy.AvatarContentType = avatar.ContentType;

                var ext = Path.GetExtension(avatar.FileName);
                var filename = Path.Combine(AvatarPath, $"{proxy.AvatarGuid}{ext}");
                using (var s = File.Open(filename, FileMode.Create))
                {
                    await avatar.CopyToAsync(s);
                }
            }
        }

        public async Task DeleteAvatarFor(Proxy proxy)
        {
            var g = proxy.AvatarGuid;
            proxy.AvatarGuid = Guid.Empty;
            proxy.AvatarContentType = string.Empty;
            await db.SaveChangesAsync();

            DeleteExistingAvatars(g);
        }

        public string PathFor(Proxy proxy)
        {
            return Directory.EnumerateFiles(AvatarPath, $"{proxy.AvatarGuid}.*").FirstOrDefault();
        }
    }
}