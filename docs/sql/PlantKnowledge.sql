USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantKnowledge]    指令碼日期: 2026/9/5 下午 11:03:05 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantKnowledge](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[SpeciesID] [uniqueidentifier] NOT NULL,
	[LightRequirement] [nvarchar](max) NULL,
	[WaterRequirement] [nvarchar](max) NULL,
	[HumidityRequirement] [nvarchar](max) NULL,
	[TemperatureMin] [decimal](5, 2) NULL,
	[TemperatureMax] [decimal](5, 2) NULL,
	[SoilRequirement] [nvarchar](max) NULL,
	[FertilizerRequirement] [nvarchar](max) NULL,
	[Dormancy] [nvarchar](max) NULL,
	[GrowthSeason] [nvarchar](max) NULL,
	[RepottingAdvice] [nvarchar](max) NULL,
	[CommonProblems] [nvarchar](max) NULL,
	[PestProblems] [nvarchar](max) NULL,
	[DiseaseProblems] [nvarchar](max) NULL,
	[CareSummary] [nvarchar](max) NULL,
	[ExternalCareGuide] [nvarchar](max) NULL,
	[SuggestedLight] [int] NULL,
	[CareTaboosJson] [nvarchar](max) NULL,
	[SuggestedWateringIntervalDays] [int] NULL,
	[SourceUpdatedAt] [datetime2](7) NULL,
	[DataVersion] [int] NOT NULL,
 CONSTRAINT [PK_PlantKnowledge_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantKnowledge] ADD  CONSTRAINT [DF_PlantKnowledge_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantKnowledge] ADD  CONSTRAINT [DF_PlantKnowledge_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantKnowledge] ADD  CONSTRAINT [DF_PlantKnowledge_DataVersion_New]  DEFAULT ((1)) FOR [DataVersion]
GO

ALTER TABLE [dbo].[PlantKnowledge]  WITH CHECK ADD  CONSTRAINT [FK_PlantKnowledge_Species_New] FOREIGN KEY([SpeciesID])
REFERENCES [dbo].[PlantSpecies] ([ID])
GO

ALTER TABLE [dbo].[PlantKnowledge] CHECK CONSTRAINT [FK_PlantKnowledge_Species_New]
GO


