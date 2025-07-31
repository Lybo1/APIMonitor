using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIMonitor.server.Migrations
{
    /// <inheritdoc />
    public partial class AddedHelperModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardWidgets");

            migrationBuilder.DropTable(
                name: "IpGeolocations");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "TokenResponses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "TokenResponses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RefreshTokenExpiry",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateTable(
                name: "LatencyMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DnsResolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Connect = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalRequest = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatencyMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiScanResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LatencyId = table.Column<int>(type: "int", nullable: false),
                    Headers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodySnippet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Health = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ColorHint = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiScanResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiScanResults_LatencyMetrics_LatencyId",
                        column: x => x.LatencyId,
                        principalTable: "LatencyMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PacketInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    DestinationIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    SourceMac = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    DestinationMac = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Length = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayloadPreview = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiScanResultId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacketInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PacketInfos_ApiScanResults_ApiScanResultId",
                        column: x => x.ApiScanResultId,
                        principalTable: "ApiScanResults",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiScanResults_LatencyId",
                table: "ApiScanResults",
                column: "LatencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PacketInfos_ApiScanResultId",
                table: "PacketInfos",
                column: "ApiScanResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PacketInfos");

            migrationBuilder.DropTable(
                name: "ApiScanResults");

            migrationBuilder.DropTable(
                name: "LatencyMetrics");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "TokenResponses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "TokenResponses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RefreshTokenExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.CreateTable(
                name: "DashboardWidgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PositionX = table.Column<int>(type: "int", nullable: false),
                    PositionY = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WidgetType = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardWidgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardWidgets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IpGeolocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ipv4Address = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Ipv6Address = table.Column<string>(type: "nvarchar(39)", maxLength: 39, nullable: false),
                    Latitude = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Longitude = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpGeolocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgets_UserId",
                table: "DashboardWidgets",
                column: "UserId");
        }
    }
}
