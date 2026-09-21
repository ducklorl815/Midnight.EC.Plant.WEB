USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantCareRecord]    指令碼日期: 2026/9/5 下午 11:01:59 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantCareRecord](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[RecordDate] [datetime2](7) NOT NULL,
	[CareType] [int] NOT NULL,
	[NumericValue] [decimal](10, 2) NULL,
	[Unit] [nvarchar](16) NULL,
	[Note] [nvarchar](max) NULL,
 CONSTRAINT [PK_PlantCareRecord_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantCareRecord] ADD  CONSTRAINT [DF_PlantCareRecord_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantCareRecord] ADD  CONSTRAINT [DF_PlantCareRecord_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantCareRecord]  WITH CHECK ADD  CONSTRAINT [FK_PlantCareRecord_Plant_New] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantCareRecord] CHECK CONSTRAINT [FK_PlantCareRecord_Plant_New]
GO


