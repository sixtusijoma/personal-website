using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialChatLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    answer = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gemini_status_code = table.Column<int>(type: "integer", nullable: true),
                    interaction_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    previous_interaction_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latency_ms = table.Column<int>(type: "integer", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_logs", x => x.id);
                });

            // Enable RLS with no policies so Supabase's public Data API (anon/authenticated roles)
            // cannot read or write this table. The backend connects as the postgres role, which bypasses RLS.
            migrationBuilder.Sql("ALTER TABLE chat_logs ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.CreateIndex(
                name: "ix_chat_logs_created_at",
                table: "chat_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_chat_logs_session_id",
                table: "chat_logs",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_logs");
        }
    }
}
