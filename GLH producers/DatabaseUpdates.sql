-- Run this script after opening the GLH Producers database.
-- It adds the simple loyalty points table used by the MVC project.

IF OBJECT_ID('dbo.UserPoints', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPoints
    (
        UserPointId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        Points INT NOT NULL DEFAULT 0,
        LastUpdated DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_UserPoints_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_UserPoints_UserId'
      AND object_id = OBJECT_ID('dbo.UserPoints')
)
BEGIN
    CREATE UNIQUE INDEX UX_UserPoints_UserId
    ON dbo.UserPoints(UserId);
END;

INSERT INTO dbo.UserPoints (UserId, Points, LastUpdated)
SELECT u.UserId, 0, GETDATE()
FROM dbo.Users u
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.UserPoints up
    WHERE up.UserId = u.UserId
);
