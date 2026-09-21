USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantSpecies]    指令碼日期: 2026/9/5 下午 11:05:31 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantSpecies](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[ScientificName] [nvarchar](256) NOT NULL,
	[CommonName] [nvarchar](256) NULL,
	[ChineseName] [nvarchar](256) NULL,
	[Genus] [nvarchar](128) NULL,
	[Family] [nvarchar](128) NULL,
	[TaxonId] [nvarchar](128) NULL,
	[ImageUrl] [nvarchar](1024) NULL,
	[SourceType] [nvarchar](64) NULL,
	[SourceId] [nvarchar](128) NULL,
 CONSTRAINT [PK_PlantSpecies_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantSpecies] ADD  CONSTRAINT [DF_PlantSpecies_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantSpecies] ADD  CONSTRAINT [DF_PlantSpecies_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO


