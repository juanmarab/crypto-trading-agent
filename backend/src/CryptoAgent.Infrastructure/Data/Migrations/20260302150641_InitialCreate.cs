using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace CryptoAgent.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "market_news",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    asset = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    headline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    sentiment_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_news", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "technical_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    asset = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    open_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    high_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    low_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    close_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    volume = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: false),
                    ema_20 = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    ema_50 = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    ema_200 = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    rsi = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    macd_line = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    macd_signal = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    macd_histogram = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    bb_upper = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    bb_middle = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    bb_lower = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    atr = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technical_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    telegram_chat_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    alert_only_assets = table.Column<int[]>(type: "integer[]", nullable: false),
                    min_confidence_threshold = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.7m),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_alerts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    asset = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    technical_verdict = table.Column<string>(type: "text", nullable: false),
                    fundamental_verdict = table.Column<string>(type: "text", nullable: false),
                    action = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    suggested_leverage = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    raw_llm_output = table.Column<string>(type: "text", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    news_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false, defaultValueSql: "'{}'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_decisions_technical_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalTable: "technical_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_agent_decisions_asset_time",
                table: "agent_decisions",
                columns: new[] { "asset", "decided_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_agent_decisions_snapshot_id",
                table: "agent_decisions",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "idx_market_news_asset_published",
                table: "market_news",
                columns: new[] { "asset", "published_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_tech_snapshots_asset_time",
                table: "technical_snapshots",
                columns: new[] { "asset", "captured_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_user_alerts_chat_id",
                table: "user_alerts",
                column: "telegram_chat_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_decisions");

            migrationBuilder.DropTable(
                name: "market_news");

            migrationBuilder.DropTable(
                name: "user_alerts");

            migrationBuilder.DropTable(
                name: "technical_snapshots");
        }
    }
}
