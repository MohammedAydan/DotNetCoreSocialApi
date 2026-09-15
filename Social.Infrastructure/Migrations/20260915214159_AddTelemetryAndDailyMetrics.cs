using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryAndDailyMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyMetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    Dau = table.Column<int>(type: "int", nullable: false),
                    NewUsersCount = table.Column<int>(type: "int", nullable: false),
                    PostsCreatedCount = table.Column<int>(type: "int", nullable: false),
                    SharesCount = table.Column<int>(type: "int", nullable: false),
                    LikesCount = table.Column<int>(type: "int", nullable: false),
                    CommentsCount = table.Column<int>(type: "int", nullable: false),
                    AvgApiLatencyMs = table.Column<double>(type: "double", nullable: false),
                    Error5xxCount = table.Column<int>(type: "int", nullable: false),
                    Error4xxCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMetricSnapshots", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RequestLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Endpoint = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HttpMethod = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<double>(type: "double", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricSnapshots_Date",
                table: "DailyMetricSnapshots",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_CreatedAt",
                table: "RequestLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_CreatedAt_StatusCode",
                table: "RequestLogs",
                columns: new[] { "CreatedAt", "StatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_Endpoint",
                table: "RequestLogs",
                column: "Endpoint");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_Endpoint_CreatedAt",
                table: "RequestLogs",
                columns: new[] { "Endpoint", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_StatusCode",
                table: "RequestLogs",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_UserId",
                table: "RequestLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyMetricSnapshots");

            migrationBuilder.DropTable(
                name: "RequestLogs");
        }
    }
}
