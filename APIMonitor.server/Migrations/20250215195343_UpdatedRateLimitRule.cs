using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIMonitor.server.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedRateLimitRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "RateLimitRules",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "RateLimitRules");
        }
    }
}
