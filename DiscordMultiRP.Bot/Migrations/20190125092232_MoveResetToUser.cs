using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordMultiRP.Bot.Migrations
{
    public partial class MoveResetToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReset",
                table: "Proxies");

            migrationBuilder.AddColumn<string>(
                name: "ResetCommand",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetCommand",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "IsReset",
                table: "Proxies",
                nullable: false,
                defaultValue: false);
        }
    }
}
