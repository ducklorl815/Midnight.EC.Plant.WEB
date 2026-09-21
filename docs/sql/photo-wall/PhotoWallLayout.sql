USE [MidnightECPlant]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.PhotoWallLayout', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PhotoWallLayout](
        [ID] [uniqueidentifier] NOT NULL,
        [Seq] [int] IDENTITY(1,1) NOT NULL,
        [CreateDate] [datetime2](7) NOT NULL,
        [ModifyDate] [datetime2](7) NOT NULL,
        [Enabled] [bit] NOT NULL,
        [Deleted] [bit] NOT NULL,
        [LayoutKey] [nvarchar](64) NOT NULL,
        [LayoutJson] [nvarchar](max) NOT NULL,
     CONSTRAINT [PK_PhotoWallLayout] PRIMARY KEY CLUSTERED ([ID] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

    ALTER TABLE [dbo].[PhotoWallLayout] ADD CONSTRAINT [DF_PhotoWallLayout_Enabled] DEFAULT ((1)) FOR [Enabled]
    ALTER TABLE [dbo].[PhotoWallLayout] ADD CONSTRAINT [DF_PhotoWallLayout_Deleted] DEFAULT ((0)) FOR [Deleted]
    CREATE UNIQUE NONCLUSTERED INDEX [UX_PhotoWallLayout_LayoutKey]
        ON [dbo].[PhotoWallLayout]([LayoutKey] ASC)
        WHERE [Deleted] = 0
END
GO
