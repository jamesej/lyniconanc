USE [<Database>]
GO

/****** Object:  Table [dbo].[ContentItems]    Script Date: 10/04/2013 09:30:28 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

ALTER TABLE ContentItems ADD
Created DATETIME NOT NULL CONSTRAINT ContentItems_Default_Created DEFAULT GETDATE(),
UserCreated UNIQUEIDENTIFIER NULL,
Updated DATETIME NOT NULL CONSTRAINT ContentItems_Default_Updated DEFAULT GETDATE(),
UserUpdated UNIQUEIDENTIFIER NULL
GO

INSERT INTO [dbo].[DbChanges]
           ([Change]
           ,[WhenChanged])
     VALUES
           ('LyniconInit 0.1'
           ,GETDATE())
GO

SET ANSI_PADDING OFF
GO




