USE TestDb;
GO
CREATE SCHEMA [User] AUTHORIZATION dbo;
GO
------------------------------------------------------------
PRINT "Creating table User.User"
CREATE TABLE [User].[User] (
    Id INT IDENTITY(1,1) NOT NULL,
    IsDeleted BIT NOT NULL,    
    CONSTRAINT [PK_User_User] PRIMARY KEY CLUSTERED ( Id ASC )
    WITH (
	    PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
GO
ALTER TABLE [User].[User] SET (LOCK_ESCALATION = TABLE)
GO
------------------------------------------------------------
PRINT "Creating table User.Address"
CREATE TABLE [User].[Address] (
    Id INT IDENTITY(1,1) NOT NULL,
    IsDeleted BIT NOT NULL,
    UserId INT NOT NULL,
    CONSTRAINT [PK_User_Address] PRIMARY KEY CLUSTERED ( Id ASC )
    WITH (
	    PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
GO
------------------------------------------------------------
PRINT "Populating test data"
------------------------------------------------------------
SET IDENTITY_INSERT [User].[User] ON
INSERT INTO [User].[User]([Id],[IsDeleted],[Status],[BrandId])
     VALUES (1,0)
SET IDENTITY_INSERT [User].[User] OFF
------------------------------------------------------------
INSERT INTO [User].[Address]([IsDeleted], [UserId])
     VALUES(0, 1)