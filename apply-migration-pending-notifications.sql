-- Migration: Add PendingNotifications table for polling-based notifications
-- This allows mobile users to receive notifications without push (FCM blocked in Cuba)
-- Date: 2026-01-30

-- Check if table doesn't exist before creating
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PendingNotifications')
BEGIN
    CREATE TABLE [PendingNotifications] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(1000) NOT NULL,
        [NotificationType] NVARCHAR(50) NOT NULL,
        [ReferenceId] INT NULL,
        [ReferenceType] NVARCHAR(50) NULL,
        [AdditionalData] NVARCHAR(MAX) NULL,
        [IsRead] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ReadAt] DATETIME2 NULL,
        CONSTRAINT [PK_PendingNotifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PendingNotifications_AspNetUsers] FOREIGN KEY ([UserId]) 
            REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
    );

    -- Index for faster queries by user and read status
    CREATE INDEX [IX_PendingNotifications_UserId_IsRead] 
        ON [PendingNotifications] ([UserId], [IsRead]);
    
    -- Index for cleanup of old notifications
    CREATE INDEX [IX_PendingNotifications_CreatedAt] 
        ON [PendingNotifications] ([CreatedAt]);

    PRINT 'Table PendingNotifications created successfully';
END
ELSE
BEGIN
    PRINT 'Table PendingNotifications already exists';
END
GO

-- Add LastActivityAt column to UserDeviceTokens for tracking mobile app activity
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserDeviceTokens') AND name = 'LastActivityAt')
BEGIN
    ALTER TABLE [UserDeviceTokens] ADD [LastActivityAt] DATETIME2 NULL;
    PRINT 'Column LastActivityAt added to UserDeviceTokens';
END
ELSE
BEGIN
    PRINT 'Column LastActivityAt already exists in UserDeviceTokens';
END
GO

-- Initialize LastActivityAt with UpdatedAt value for existing records (separate batch)
UPDATE [UserDeviceTokens] SET [LastActivityAt] = [UpdatedAt] WHERE [LastActivityAt] IS NULL;
GO
