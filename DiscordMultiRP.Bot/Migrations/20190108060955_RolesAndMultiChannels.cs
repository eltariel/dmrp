using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordMultiRP.Bot.Migrations
{
    public partial class RolesAndMultiChannels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsMonitored",
                table: "Channels",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProxyChannels",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProxyId = table.Column<int>(nullable: false),
                    ChannelId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxyChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProxyChannels_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProxyChannels_Proxies_ProxyId",
                        column: x => x.ProxyId,
                        principalTable: "Proxies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProxyChannels_ChannelId",
                table: "ProxyChannels",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProxyChannels_ProxyId",
                table: "ProxyChannels",
                column: "ProxyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProxyChannels");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsMonitored",
                table: "Channels");
        }
    }
}
