using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social.Infrastucture.Migrations
{
    /// <inheritdoc />
    public partial class AddNewCollForUserForUserGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserGender",
                table: "AspNetUsers",
                type: "longtext",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserGender",
                table: "AspNetUsers");
        }
    }
}
