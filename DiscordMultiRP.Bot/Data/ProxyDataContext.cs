using DiscordMultiRP.Bot.Proxy;
using Microsoft.EntityFrameworkCore;

namespace DiscordMultiRP.Bot.Data
{
    public class ProxyDataContext : DbContext
    {
        public ProxyDataContext() : base(new DbContextOptionsBuilder()
            .UseSqlServer("Server=.;Database=DiscordMultiRP;Trusted_Connection=True;").Options)
        {
        }

        public ProxyDataContext(DbContextOptions opt) : base(opt)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Proxy> Proxies { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<UserChannel> UserChannels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Proxy>()
                .HasOne(p => p.Channel)
                .WithMany()
                .IsRequired(false);
        }
    }
}