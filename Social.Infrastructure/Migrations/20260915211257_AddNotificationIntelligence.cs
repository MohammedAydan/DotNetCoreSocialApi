using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActorCount",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "GroupKey",
                table: "Notifications",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeferred",
                table: "Notifications",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastActorName",
                table: "Notifications",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LikeEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CommentEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReplyEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FollowEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FollowRequestEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShareEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MentionEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    QuietStartHourUtc = table.Column<int>(type: "int", nullable: true),
                    QuietEndHourUtc = table.Column<int>(type: "int", nullable: true),
                    DigestEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_GroupKey_IsRead",
                table: "Notifications",
                columns: new[] { "RecipientId", "GroupKey", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_IsRead_Priority_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "IsRead", "Priority", "CreatedAt" },
                descending: new[] { false, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId_GroupKey_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId_IsRead_Priority_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActorCount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "GroupKey",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsDeferred",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "LastActorName",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Notifications");
        }
    }
}
