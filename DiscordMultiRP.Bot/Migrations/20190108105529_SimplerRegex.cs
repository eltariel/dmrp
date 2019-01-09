using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordMultiRP.Bot.Migrations
{
    public partial class SimplerRegex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Regex",
                table: "Proxies",
                newName: "Suffix");

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "Proxies",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "Proxies");

            migrationBuilder.RenameColumn(
                name: "Suffix",
                table: "Proxies",
                newName: "Regex");
        }
    }
}
