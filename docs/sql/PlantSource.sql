USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantSource]    指令碼日期: 2026/9/5 下午 11:04:38 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantSource](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[SpeciesID] [uniqueidentifier] NOT NULL,
	[SourceType] [int] NOT NULL,
	[Title] [nvarchar](512) NULL,
	[Url] [nvarchar](2048) NOT NULL,
	[Domain] [nvarchar](256) NULL,
	[Author] [nvarchar](256) NULL,
	[PublishedAt] [datetime2](7) NULL,
	[Language] [nvarchar](16) NULL,
	[ContentHash] [nvarchar](64) NULL,
	[ReliabilityLevel] [int] NOT NULL,
 CONSTRAINT [PK_PlantSource_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantSource] ADD  CONSTRAINT [DF_PlantSource_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantSource] ADD  CONSTRAINT [DF_PlantSource_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantSource] ADD  CONSTRAINT [DF_PlantSource_Reliability_New]  DEFAULT ((3)) FOR [ReliabilityLevel]
GO

ALTER TABLE [dbo].[PlantSource]  WITH CHECK ADD  CONSTRAINT [FK_PlantSource_Species_New] FOREIGN KEY([SpeciesID])
REFERENCES [dbo].[PlantSpecies] ([ID])
GO

ALTER TABLE [dbo].[PlantSource] CHECK CONSTRAINT [FK_PlantSource_Species_New]
GO


