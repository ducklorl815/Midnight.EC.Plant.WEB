USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantAnalysisJob]    指令碼日期: 2026/9/5 下午 11:01:46 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantAnalysisJob](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[DiaryID] [uniqueidentifier] NULL,
	[ImageID] [uniqueidentifier] NULL,
	[AnalysisID] [uniqueidentifier] NULL,
	[Status] [int] NOT NULL,
	[AnalysisScope] [int] NOT NULL,
	[RetryCount] [int] NOT NULL,
	[StartedAt] [datetime2](7) NULL,
	[CompletedAt] [datetime2](7) NULL,
	[ErrorMessage] [nvarchar](max) NULL,
 CONSTRAINT [PK_PlantAnalysisJob_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantAnalysisJob] ADD  CONSTRAINT [DF_PlantAnalysisJob_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantAnalysisJob] ADD  CONSTRAINT [DF_PlantAnalysisJob_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantAnalysisJob] ADD  CONSTRAINT [DF_PlantAnalysisJob_Retry_New]  DEFAULT ((0)) FOR [RetryCount]
GO

ALTER TABLE [dbo].[PlantAnalysisJob]  WITH CHECK ADD  CONSTRAINT [FK_PlantAnalysisJob_Analysis_New] FOREIGN KEY([AnalysisID])
REFERENCES [dbo].[PlantAnalysis] ([ID])
GO

ALTER TABLE [dbo].[PlantAnalysisJob] CHECK CONSTRAINT [FK_PlantAnalysisJob_Analysis_New]
GO

ALTER TABLE [dbo].[PlantAnalysisJob]  WITH CHECK ADD  CONSTRAINT [FK_PlantAnalysisJob_Diary_New] FOREIGN KEY([DiaryID])
REFERENCES [dbo].[PlantDiary] ([ID])
GO

ALTER TABLE [dbo].[PlantAnalysisJob] CHECK CONSTRAINT [FK_PlantAnalysisJob_Diary_New]
GO

ALTER TABLE [dbo].[PlantAnalysisJob]  WITH CHECK ADD  CONSTRAINT [FK_PlantAnalysisJob_Image_New] FOREIGN KEY([ImageID])
REFERENCES [dbo].[PlantImage] ([ID])
GO

ALTER TABLE [dbo].[PlantAnalysisJob] CHECK CONSTRAINT [FK_PlantAnalysisJob_Image_New]
GO

ALTER TABLE [dbo].[PlantAnalysisJob]  WITH CHECK ADD  CONSTRAINT [FK_PlantAnalysisJob_Plant_New] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantAnalysisJob] CHECK CONSTRAINT [FK_PlantAnalysisJob_Plant_New]
GO


