using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIMonitor.server.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Endpoint",
                table: "ApiMetrics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Endpoint",
                table: "ApiMetrics");
        }
    }
}
