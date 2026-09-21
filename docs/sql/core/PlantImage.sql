USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantImage]    指令碼日期: 2026/9/5 下午 11:02:49 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantImage](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[DiaryID] [uniqueidentifier] NULL,
	[Note] [nvarchar](2000) NULL,
	[IsCover] [bit] NOT NULL,
	[FileName] [nvarchar](512) NOT NULL,
	[StoragePath] [nvarchar](1024) NOT NULL,
	[ThumbnailPath] [nvarchar](1024) NULL,
	[OriginalFileName] [nvarchar](512) NULL,
	[ContentType] [nvarchar](128) NULL,
	[Width] [int] NULL,
	[Height] [int] NULL,
	[FileSize] [bigint] NOT NULL,
	[Sha256] [nvarchar](64) NULL,
 CONSTRAINT [PK_PlantImage_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantImage] ADD  CONSTRAINT [DF_PlantImage_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantImage] ADD  CONSTRAINT [DF_PlantImage_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantImage] ADD  CONSTRAINT [DF_PlantImage_IsCover_New]  DEFAULT ((0)) FOR [IsCover]
GO

ALTER TABLE [dbo].[PlantImage] ADD  CONSTRAINT [DF_PlantImage_FileSize_New]  DEFAULT ((0)) FOR [FileSize]
GO

ALTER TABLE [dbo].[PlantImage]  WITH CHECK ADD  CONSTRAINT [FK_PlantImage_Diary_New] FOREIGN KEY([DiaryID])
REFERENCES [dbo].[PlantDiary] ([ID])
GO

ALTER TABLE [dbo].[PlantImage] CHECK CONSTRAINT [FK_PlantImage_Diary_New]
GO

ALTER TABLE [dbo].[PlantImage]  WITH CHECK ADD  CONSTRAINT [FK_PlantImage_Plant_New] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantImage] CHECK CONSTRAINT [FK_PlantImage_Plant_New]
GO


