using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIpAddressWithIpHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop raw addresses instead of renaming. Existing values must not be kept as ip_hash.
            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "chat_logs");

            migrationBuilder.AddColumn<string>(
                name: "ip_hash",
                table: "chat_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ip_hash",
                table: "chat_logs");

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "chat_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
