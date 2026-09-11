USE [MidnightECPlant]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.PageComposerLayout', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PageComposerLayout](
        [ID] [uniqueidentifier] NOT NULL,
        [Seq] [int] IDENTITY(1,1) NOT NULL,
        [CreateDate] [datetime2](7) NOT NULL,
        [ModifyDate] [datetime2](7) NOT NULL,
        [Enabled] [bit] NOT NULL,
        [Deleted] [bit] NOT NULL,
        [LayoutKey] [nvarchar](64) NOT NULL,
        [LayoutJson] [nvarchar](max) NOT NULL,
     CONSTRAINT [PK_PageComposerLayout] PRIMARY KEY CLUSTERED ([ID] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

    ALTER TABLE [dbo].[PageComposerLayout] ADD CONSTRAINT [DF_PageComposerLayout_Enabled] DEFAULT ((1)) FOR [Enabled]
    ALTER TABLE [dbo].[PageComposerLayout] ADD CONSTRAINT [DF_PageComposerLayout_Deleted] DEFAULT ((0)) FOR [Deleted]
END
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.PageComposerLayout', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_PageComposerLayout_LayoutKey'
          AND object_id = OBJECT_ID(N'dbo.PageComposerLayout')
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_PageComposerLayout_LayoutKey]
        ON [dbo].[PageComposerLayout]([LayoutKey] ASC)
        WHERE [Deleted] = 0
END
GO
