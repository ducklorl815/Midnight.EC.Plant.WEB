USE [MidnightECPlant]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantFertilizerProduct](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](64) NOT NULL,
	[IntervalDays] [int] NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_PlantFertilizerProduct] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantFertilizerProduct] ADD CONSTRAINT [DF_PlantFertilizerProduct_Enabled] DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantFertilizerProduct] ADD CONSTRAINT [DF_PlantFertilizerProduct_Deleted] DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantFertilizerProduct] ADD CONSTRAINT [DF_PlantFertilizerProduct_SortOrder] DEFAULT ((0)) FOR [SortOrder]
GO

ALTER TABLE [dbo].[PlantFertilizerProduct] WITH CHECK ADD CONSTRAINT [FK_PlantFertilizerProduct_Plant] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantFertilizerProduct] CHECK CONSTRAINT [FK_PlantFertilizerProduct_Plant]
GO

-- CareRecord：施肥紀錄可掛盆用肥料
IF COL_LENGTH('dbo.PlantCareRecord', 'FertilizerProductID') IS NULL
BEGIN
	ALTER TABLE [dbo].[PlantCareRecord] ADD [FertilizerProductID] [uniqueidentifier] NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PlantCareRecord_FertilizerProduct')
BEGIN
	ALTER TABLE [dbo].[PlantCareRecord] WITH CHECK ADD CONSTRAINT [FK_PlantCareRecord_FertilizerProduct]
	FOREIGN KEY([FertilizerProductID]) REFERENCES [dbo].[PlantFertilizerProduct] ([ID]);
END
GO
