CREATE DATABASE TrustVaultDB;
GO
USE TrustVaultDB;
GO

CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(254) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role NVARCHAR(20) NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'Owner', 'Viewer')),
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.Documents (
    DocumentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Documents PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    StoredFileName NVARCHAR(100) NOT NULL,
    OriginalFileName NVARCHAR(255) NOT NULL,
    OwnerId INT NOT NULL,
    UploadDate DATETIME2 NOT NULL CONSTRAINT DF_Documents_UploadDate DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Documents_Users FOREIGN KEY (OwnerId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
);
GO
CREATE INDEX IX_Documents_OwnerId_UploadDate ON dbo.Documents(OwnerId, UploadDate DESC);
GO

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
GO
CREATE INDEX IX_DocumentPermissions_DocumentId_UserId ON dbo.DocumentPermissions(DocumentId, UserId);
GO

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
GO
CREATE INDEX IX_AuditLogs_Timestamp ON dbo.AuditLogs(Timestamp DESC);
GO
