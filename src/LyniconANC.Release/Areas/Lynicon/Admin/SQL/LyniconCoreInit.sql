USE [<Database>]
GO

/****** Object:  Table [dbo].[ContentItems]    Script Date: 10/04/2013 09:30:28 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ContentItems](
	[Id] [uniqueidentifier] NOT NULL,
	[Identity] [uniqueidentifier] NOT NULL,
	[DataType] [varchar](250) NOT NULL,
	[Path] [nvarchar](250) NULL,
	[Locale] [varchar](10) NULL,
	[Summary] [nvarchar](max) NULL,
	[Content] [nvarchar](max) NULL,
	[Title] [nvarchar](250) NULL,
	[Created] [datetime] NOT NULL,
	[UserCreated] [varchar](40) NULL,
	[Updated] [datetime] NOT NULL,
	[UserUpdated] [varchar](40) NULL,
 CONSTRAINT [PK_ContentItems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE TABLE [dbo].[Users](
	[Id] [uniqueidentifier] NOT NULL,
	[UserName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](128) NULL,
	[Password] [nvarchar](128) NULL,
	[Roles] [varchar](30) NULL,
	[Created] [date] NOT NULL,
	[Modified] [date] NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[DbChanges]    Script Date: 11/18/2013 11:50:58 ******/

CREATE TABLE [dbo].[DbChanges](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Change] [nvarchar](100) NOT NULL,
	[ChangedWhen] [datetime] NOT NULL,
 CONSTRAINT [PK_DbChanges] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

INSERT INTO [dbo].[DbChanges]
           ([Change]
           ,[ChangedWhen])
     VALUES
           ('LyniconInit 0.1'
           ,GETDATE())
GO

SET ANSI_PADDING OFF
GO




