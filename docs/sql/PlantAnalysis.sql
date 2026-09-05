USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantAnalysis]    指令碼日期: 2026/9/5 下午 11:01:22 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantAnalysis](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[DiaryID] [uniqueidentifier] NULL,
	[ImageID] [uniqueidentifier] NULL,
	[AnalysisType] [int] NOT NULL,
	[AnalysisScope] [int] NOT NULL,
	[ModelName] [nvarchar](128) NULL,
	[PromptVersion] [nvarchar](64) NULL,
	[InputSnapshot] [nvarchar](max) NULL,
	[ResultJson] [nvarchar](max) NULL,
	[Summary] [nvarchar](2000) NULL,
	[HealthScore] [int] NULL,
	[Confidence] [decimal](5, 4) NULL,
 CONSTRAINT [PK_PlantAnalysis_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantAnalysis] ADD  CONSTRAINT [DF_PlantAnalysis_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantAnalysis] ADD  CONSTRAINT [DF_PlantAnalysis_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantAnalysis]  WITH CHECK ADD  CONSTRAINT [FK_PlantAnalysis_Diary_New] FOREIGN KEY([DiaryID])
REFERENCES [dbo].[PlantDiary] ([ID])
GO

ALTER TABLE [dbo].[PlantAnalysis] CHECK CONSTRAINT [FK_PlantAnalysis_Diary_New]
GO

ALTER TABLE [dbo].[PlantAnalysis]  WITH CHECK ADD  CONSTRAINT [FK_PlantAnalysis_Image_New] FOREIGN KEY([ImageID])
REFERENCES [dbo].[PlantImage] ([ID])
GO

ALTER TABLE [dbo].[PlantAnalysis] CHECK CONSTRAINT [FK_PlantAnalysis_Image_New]
GO

ALTER TABLE [dbo].[PlantAnalysis]  WITH CHECK ADD  CONSTRAINT [FK_PlantAnalysis_Plant_New] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantAnalysis] CHECK CONSTRAINT [FK_PlantAnalysis_Plant_New]
GO


