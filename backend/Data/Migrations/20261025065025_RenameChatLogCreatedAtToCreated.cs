using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameChatLogCreatedAtToCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "chat_logs",
                newName: "created");

            migrationBuilder.RenameIndex(
                name: "ix_chat_logs_created_at",
                table: "chat_logs",
                newName: "ix_chat_logs_created");

            migrationBuilder.RenameColumn(
                name: "latency_ms",
                table: "chat_logs",
                newName: "latency");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "latency",
                table: "chat_logs",
                newName: "latency_ms");

            migrationBuilder.RenameColumn(
                name: "created",
                table: "chat_logs",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "ix_chat_logs_created",
                table: "chat_logs",
                newName: "ix_chat_logs_created_at");
        }
    }
}
