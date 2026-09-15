using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safely drop foreign keys if they exist in MySQL.
            // This handles schema drift where previous aborted migrations already dropped them in MySQL.
            migrationBuilder.Sql(@"
                DROP PROCEDURE IF EXISTS `drop_fk_if_exists`;
                CREATE PROCEDURE `drop_fk_if_exists`(IN tbl VARCHAR(64), IN fk VARCHAR(64))
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                        WHERE CONSTRAINT_SCHEMA = DATABASE()
                          AND TABLE_NAME = tbl
                          AND CONSTRAINT_NAME = fk
                          AND CONSTRAINT_TYPE = 'FOREIGN KEY'
                    ) THEN
                        SET @s = CONCAT('ALTER TABLE `', tbl, '` DROP FOREIGN KEY `', fk, '`');
                        PREPARE stmt FROM @s;
                        EXECUTE stmt;
                        DEALLOCATE PREPARE stmt;
                    END IF;
                END;
                CALL `drop_fk_if_exists`('BlockUsers', 'FK_BlockUsers_AspNetUsers_BlockedUserId');
                CALL `drop_fk_if_exists`('BlockUsers', 'FK_BlockUsers_AspNetUsers_UserId');
                CALL `drop_fk_if_exists`('Comments', 'FK_Comments_Comments_ParentId');
                CALL `drop_fk_if_exists`('Posts', 'FK_Posts_Posts_ParentPostId');
                DROP PROCEDURE `drop_fk_if_exists`;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "varchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Visibility",
                table: "Posts",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CreatedAt_Id",
                table: "Posts",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

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

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId_PostId",
                table: "Likes",
                columns: new[] { "UserId", "PostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Followers_FollowerId_Accepted_FollowingId",
                table: "Followers",
                columns: new[] { "FollowerId", "Accepted", "FollowingId" });

            migrationBuilder.CreateIndex(
                name: "IX_Followers_FollowerId_FollowingId",
                table: "Followers",
                columns: new[] { "FollowerId", "FollowingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Followers_FollowingId_Accepted",
                table: "Followers",
                columns: new[] { "FollowingId", "Accepted" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId_CreatedAt",
                table: "Comments",
                columns: new[] { "PostId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_BlockUsers_BlockedUserId_UserId",
                table: "BlockUsers",
                columns: new[] { "BlockedUserId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_BlockUsers_AspNetUsers_BlockedUserId",
                table: "BlockUsers",
                column: "BlockedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BlockUsers_AspNetUsers_UserId",
                table: "BlockUsers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Comments_ParentId",
                table: "Comments",
                column: "ParentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_ParentPostId",
                table: "Posts",
                column: "ParentPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Safely drop foreign keys if they exist before restoring previous configuration
            migrationBuilder.Sql(@"
                DROP PROCEDURE IF EXISTS `drop_fk_if_exists`;
                CREATE PROCEDURE `drop_fk_if_exists`(IN tbl VARCHAR(64), IN fk VARCHAR(64))
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                        WHERE CONSTRAINT_SCHEMA = DATABASE()
                          AND TABLE_NAME = tbl
                          AND CONSTRAINT_NAME = fk
                          AND CONSTRAINT_TYPE = 'FOREIGN KEY'
                    ) THEN
                        SET @s = CONCAT('ALTER TABLE `', tbl, '` DROP FOREIGN KEY `', fk, '`');
                        PREPARE stmt FROM @s;
                        EXECUTE stmt;
                        DEALLOCATE PREPARE stmt;
                    END IF;
                END;
                CALL `drop_fk_if_exists`('BlockUsers', 'FK_BlockUsers_AspNetUsers_BlockedUserId');
                CALL `drop_fk_if_exists`('BlockUsers', 'FK_BlockUsers_AspNetUsers_UserId');
                CALL `drop_fk_if_exists`('Comments', 'FK_Comments_Comments_ParentId');
                CALL `drop_fk_if_exists`('Posts', 'FK_Posts_Posts_ParentPostId');
                DROP PROCEDURE `drop_fk_if_exists`;
            ");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CreatedAt_Id",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Visibility_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Likes_UserId_PostId",
                table: "Likes");

            migrationBuilder.DropIndex(
                name: "IX_Followers_FollowerId_Accepted_FollowingId",
                table: "Followers");

            migrationBuilder.DropIndex(
                name: "IX_Followers_FollowerId_FollowingId",
                table: "Followers");

            migrationBuilder.DropIndex(
                name: "IX_Followers_FollowingId_Accepted",
                table: "Followers");

            migrationBuilder.DropIndex(
                name: "IX_Comments_PostId_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_BlockUsers_BlockedUserId_UserId",
                table: "BlockUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(512)",
                oldMaxLength: 512)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Visibility",
                table: "Posts",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockUsers_AspNetUsers_BlockedUserId",
                table: "BlockUsers",
                column: "BlockedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BlockUsers_AspNetUsers_UserId",
                table: "BlockUsers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Comments_ParentId",
                table: "Comments",
                column: "ParentId",
                principalTable: "Comments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_ParentPostId",
                table: "Posts",
                column: "ParentPostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }
    }
}
