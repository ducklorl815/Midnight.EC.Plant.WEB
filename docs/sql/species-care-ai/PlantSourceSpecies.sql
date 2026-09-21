USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantSourceSpecies]    指令碼日期: 2026/9/5 下午 11:05:18 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantSourceSpecies](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[SourceID] [uniqueidentifier] NOT NULL,
	[SpeciesID] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_PlantSourceSpecies_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantSourceSpecies] ADD  CONSTRAINT [DF_PlantSourceSpecies_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantSourceSpecies] ADD  CONSTRAINT [DF_PlantSourceSpecies_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantSourceSpecies]  WITH CHECK ADD  CONSTRAINT [FK_PlantSourceSpecies_Source_New] FOREIGN KEY([SourceID])
REFERENCES [dbo].[PlantSource] ([ID])
GO

ALTER TABLE [dbo].[PlantSourceSpecies] CHECK CONSTRAINT [FK_PlantSourceSpecies_Source_New]
GO

ALTER TABLE [dbo].[PlantSourceSpecies]  WITH CHECK ADD  CONSTRAINT [FK_PlantSourceSpecies_Species_New] FOREIGN KEY([SpeciesID])
REFERENCES [dbo].[PlantSpecies] ([ID])
GO

ALTER TABLE [dbo].[PlantSourceSpecies] CHECK CONSTRAINT [FK_PlantSourceSpecies_Species_New]
GO


