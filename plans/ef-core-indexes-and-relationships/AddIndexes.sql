START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN


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
                

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    ALTER TABLE `RefreshTokens` MODIFY COLUMN `Token` varchar(512) CHARACTER SET utf8mb4 NOT NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    ALTER TABLE `Posts` MODIFY COLUMN `Visibility` varchar(20) CHARACTER SET utf8mb4 NOT NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE UNIQUE INDEX `IX_RefreshTokens_Token` ON `RefreshTokens` (`Token`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE INDEX `IX_Posts_CreatedAt_Id` ON `Posts` (`CreatedAt` DESC, `Id` DESC);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE INDEX `IX_Posts_UserId_CreatedAt` ON `Posts` (`UserId`, `CreatedAt` DESC);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE INDEX `IX_Posts_Visibility_CreatedAt` ON `Posts` (`Visibility`, `CreatedAt` DESC);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE INDEX `IX_Notifications_UserId_IsRead_CreatedAt` ON `Notifications` (`UserId`, `IsRead`, `CreatedAt` DESC);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE UNIQUE INDEX `IX_Likes_UserId_PostId` ON `Likes` (`UserId`, `PostId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE INDEX `IX_Followers_FollowerId_Accepted_FollowingId` ON `Followers` (`FollowerId`, `Accepted`, `FollowingId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE UNIQUE INDEX `IX_Followers_FollowerId_FollowingId` ON `Followers` (`FollowerId`, `FollowingId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE INDEX `IX_Followers_FollowingId_Accepted` ON `Followers` (`FollowingId`, `Accepted`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE INDEX `IX_Comments_PostId_CreatedAt` ON `Comments` (`PostId`, `CreatedAt` DESC);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    CREATE INDEX `IX_BlockUsers_BlockedUserId_UserId` ON `BlockUsers` (`BlockedUserId`, `UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    ALTER TABLE `BlockUsers` ADD CONSTRAINT `FK_BlockUsers_AspNetUsers_BlockedUserId` FOREIGN KEY (`BlockedUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    ALTER TABLE `BlockUsers` ADD CONSTRAINT `FK_BlockUsers_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    ALTER TABLE `Comments` ADD CONSTRAINT `FK_Comments_Comments_ParentId` FOREIGN KEY (`ParentId`) REFERENCES `Comments` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    ALTER TABLE `Posts` ADD CONSTRAINT `FK_Posts_Posts_ParentPostId` FOREIGN KEY (`ParentPostId`) REFERENCES `Posts` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260915163717_AddIndexes') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260915163717_AddIndexes', '9.0.4');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

