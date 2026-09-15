using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TunePostFeedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Visibility_CreatedAt",
                table: "Posts");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId_CreatedAt_Id",
                table: "Posts",
                columns: new[] { "UserId", "CreatedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Visibility_CreatedAt_Id",
                table: "Posts",
                columns: new[] { "Visibility", "CreatedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId_CreatedAt_Id",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Visibility_CreatedAt_Id",
                table: "Posts");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId_CreatedAt",
                table: "Posts",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Visibility_CreatedAt",
                table: "Posts",
                columns: new[] { "Visibility", "CreatedAt" },
                descending: new[] { false, true });
        }
    }
}
