using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordMultiRP.Bot.Migrations
{
    public partial class ProxyAvatarSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "Proxies",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "Proxies");
        }
    }
}
