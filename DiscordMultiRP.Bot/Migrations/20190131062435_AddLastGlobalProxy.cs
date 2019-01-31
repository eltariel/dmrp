using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordMultiRP.Bot.Migrations
{
    public partial class AddLastGlobalProxy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastGlobalProxyId",
                table: "BotUsers",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BotUsers_LastGlobalProxyId",
                table: "BotUsers",
                column: "LastGlobalProxyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BotUsers_Proxies_LastGlobalProxyId",
                table: "BotUsers",
                column: "LastGlobalProxyId",
                principalTable: "Proxies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BotUsers_Proxies_LastGlobalProxyId",
                table: "BotUsers");

            migrationBuilder.DropIndex(
                name: "IX_BotUsers_LastGlobalProxyId",
                table: "BotUsers");

            migrationBuilder.DropColumn(
                name: "LastGlobalProxyId",
                table: "BotUsers");
        }
    }
}
