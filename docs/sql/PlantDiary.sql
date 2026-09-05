USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantDiary]    指令碼日期: 2026/9/5 下午 11:02:22 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantDiary](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[DiaryDate] [datetime2](7) NOT NULL,
	[Title] [nvarchar](256) NULL,
	[Note] [nvarchar](max) NULL,
	[WeatherNote] [nvarchar](max) NULL,
	[EnvironmentNote] [nvarchar](max) NULL,
	[WateringNote] [nvarchar](max) NULL,
	[FertilizerNote] [nvarchar](max) NULL,
 CONSTRAINT [PK_PlantDiary_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantDiary] ADD  CONSTRAINT [DF_PlantDiary_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantDiary] ADD  CONSTRAINT [DF_PlantDiary_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantDiary]  WITH CHECK ADD  CONSTRAINT [FK_PlantDiary_Plant_New] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantDiary] CHECK CONSTRAINT [FK_PlantDiary_Plant_New]
GO


