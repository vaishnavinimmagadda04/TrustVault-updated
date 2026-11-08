USE TrustVaultDB;
GO

IF OBJECT_ID('dbo.DocumentPermissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentPermissions (
        PermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentPermissions PRIMARY KEY,
        DocumentId INT NOT NULL,
        UserId INT NOT NULL,
        PermissionType NVARCHAR(10) NOT NULL CONSTRAINT CK_DocumentPermissions_Type CHECK (PermissionType IN ('READ', 'EDIT')),
        GrantedBy INT NOT NULL,
        GrantedDate DATETIME2 NOT NULL CONSTRAINT DF_DocumentPermissions_GrantedDate DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL CONSTRAINT DF_DocumentPermissions_IsActive DEFAULT (1),
        CONSTRAINT FK_DocumentPermissions_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.Documents(DocumentId) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentPermissions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_DocumentPermissions_GrantedBy FOREIGN KEY (GrantedBy) REFERENCES dbo.Users(UserId)
    );
END;
GO

IF COL_LENGTH('dbo.DocumentPermissions', 'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentPermissions ADD IsActive BIT NOT NULL
        CONSTRAINT DF_DocumentPermissions_IsActive DEFAULT (1) WITH VALUES;
END;
GO

IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        LogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        UserId INT NULL,
        Action NVARCHAR(40) NOT NULL,
        DocumentId INT NULL,
        WasAllowed BIT NOT NULL,
        Timestamp DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_Timestamp DEFAULT SYSUTCDATETIME(),
        IpAddress NVARCHAR(45) NULL,
        Details NVARCHAR(1000) NULL,
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.DocumentPermissions') AND name = 'IX_DocumentPermissions_DocumentId_UserId')
    CREATE INDEX IX_DocumentPermissions_DocumentId_UserId ON dbo.DocumentPermissions(DocumentId, UserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AuditLogs') AND name = 'IX_AuditLogs_Timestamp')
    CREATE INDEX IX_AuditLogs_Timestamp ON dbo.AuditLogs(Timestamp DESC);
GO
