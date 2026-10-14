-- =================================================================================
-- SQL Server Table Creation Script for Firebase Cloud Messaging (FCM) Device Tokens
-- Database: NotificationDB
-- Table:    dbo.UserFcmDevices
-- =================================================================================

IF OBJECT_ID(N'dbo.UserFcmDevices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserFcmDevices
    (
        Id            BIGINT IDENTITY(1,1) NOT NULL,
        UserId        NVARCHAR(100)        NOT NULL,
        DeviceId      NVARCHAR(200)        NOT NULL,
        FcmToken      NVARCHAR(2048)       NOT NULL,
        Platform      NVARCHAR(20)         NOT NULL,
        IsActive      BIT                  NOT NULL CONSTRAINT DF_UserFcmDevices_IsActive DEFAULT (1),
        CreatedAtUtc  DATETIME2            NOT NULL CONSTRAINT DF_UserFcmDevices_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc  DATETIME2            NOT NULL CONSTRAINT DF_UserFcmDevices_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        LastSeenAtUtc DATETIME2            NULL,

        CONSTRAINT PK_UserFcmDevices PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT UQ_UserFcmDevices_UserId_DeviceId UNIQUE NONCLUSTERED (UserId ASC, DeviceId ASC)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_UserFcmDevices_UserId_IsActive'
      AND object_id = OBJECT_ID(N'dbo.UserFcmDevices', N'U')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserFcmDevices_UserId_IsActive
        ON dbo.UserFcmDevices (UserId ASC, IsActive ASC)
        INCLUDE (DeviceId, FcmToken, Platform, UpdatedAtUtc, LastSeenAtUtc);
END;
GO
