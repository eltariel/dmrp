using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordMultiRP.Bot.Migrations
{
    public partial class RemoveSingleChannel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proxies_Channels_ChannelId",
                table: "Proxies");

            migrationBuilder.DropIndex(
                name: "IX_Proxies_ChannelId",
                table: "Proxies");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "Proxies");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                table: "Proxies",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proxies_ChannelId",
                table: "Proxies",
                column: "ChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proxies_Channels_ChannelId",
                table: "Proxies",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
