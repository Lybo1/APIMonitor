using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIMonitor.server.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedBotDetectionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "BotDetectionLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestTimestamp",
                table: "AuditLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "ResponseTimeMs",
                table: "AuditLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "BotDetectionLogs");

            migrationBuilder.DropColumn(
                name: "RequestTimestamp",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ResponseTimeMs",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditLogs");
        }
    }
}
