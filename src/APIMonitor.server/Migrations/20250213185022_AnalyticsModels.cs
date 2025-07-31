using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIMonitor.server.Migrations
{
    /// <inheritdoc />
    public partial class AnalyticsModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalRequests = table.Column<int>(type: "int", nullable: false),
                    RequestsPerMinute = table.Column<int>(type: "int", nullable: false),
                    AverageResponseTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ErrorsCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeSlot = table.Column<TimeSpan>(type: "time", nullable: false),
                    AverageRequestsCount = table.Column<int>(type: "int", nullable: false),
                    ErrorsCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestStatistics", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiMetrics");

            migrationBuilder.DropTable(
                name: "RequestStatistics");
        }
    }
}
