USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantKnowledgeSyncLog]    指令碼日期: 2026/9/5 下午 11:03:49 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantKnowledgeSyncLog](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[SpeciesID] [uniqueidentifier] NOT NULL,
	[Provider] [int] NOT NULL,
	[RequestUrl] [nvarchar](2048) NULL,
	[Status] [int] NOT NULL,
	[ResponseHash] [nvarchar](64) NULL,
	[StartedAt] [datetime2](7) NOT NULL,
	[CompletedAt] [datetime2](7) NULL,
	[ErrorMessage] [nvarchar](max) NULL,
 CONSTRAINT [PK_PlantKnowledgeSyncLog_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantKnowledgeSyncLog] ADD  CONSTRAINT [DF_PlantKnowledgeSyncLog_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantKnowledgeSyncLog] ADD  CONSTRAINT [DF_PlantKnowledgeSyncLog_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantKnowledgeSyncLog]  WITH CHECK ADD  CONSTRAINT [FK_PlantKnowledgeSyncLog_Species_New] FOREIGN KEY([SpeciesID])
REFERENCES [dbo].[PlantSpecies] ([ID])
GO

ALTER TABLE [dbo].[PlantKnowledgeSyncLog] CHECK CONSTRAINT [FK_PlantKnowledgeSyncLog_Species_New]
GO


