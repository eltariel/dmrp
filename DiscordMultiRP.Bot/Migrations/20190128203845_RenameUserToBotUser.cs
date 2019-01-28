using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordMultiRP.Bot.Migrations
{
    public partial class RenameUserToBotUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proxies_Users_UserId",
                table: "Proxies");

            migrationBuilder.DropForeignKey(
                name: "FK_UserChannels_Users_UserId",
                table: "UserChannels");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UserChannels",
                newName: "BotUserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserChannels_UserId",
                table: "UserChannels",
                newName: "IX_UserChannels_BotUserId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Proxies",
                newName: "BotUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Proxies_UserId",
                table: "Proxies",
                newName: "IX_Proxies_BotUserId");

            migrationBuilder.RenameTable(name: "Users", newName: "BotUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_Proxies_BotUsers_BotUserId",
                table: "Proxies",
                column: "BotUserId",
                principalTable: "BotUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserChannels_BotUsers_BotUserId",
                table: "UserChannels",
                column: "BotUserId",
                principalTable: "BotUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proxies_BotUsers_BotUserId",
                table: "Proxies");

            migrationBuilder.DropForeignKey(
                name: "FK_UserChannels_BotUsers_BotUserId",
                table: "UserChannels");

            migrationBuilder.RenameColumn(
                name: "BotUserId",
                table: "UserChannels",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserChannels_BotUserId",
                table: "UserChannels",
                newName: "IX_UserChannels_UserId");

            migrationBuilder.RenameColumn(
                name: "BotUserId",
                table: "Proxies",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Proxies_BotUserId",
                table: "Proxies",
                newName: "IX_Proxies_UserId");

            migrationBuilder.RenameTable(name: "BotUsers", newName: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_Proxies_Users_UserId",
                table: "Proxies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserChannels_Users_UserId",
                table: "UserChannels",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
