USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantProfile]    指令碼日期: 2026/9/5 下午 11:04:08 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantProfile](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[WateringIntervalDays] [int] NULL,
	[FertilizingIntervalDays] [int] NULL,
	[TargetHumidityMin] [decimal](5, 2) NULL,
	[TargetHumidityMax] [decimal](5, 2) NULL,
	[TargetTemperatureMin] [decimal](5, 2) NULL,
	[TargetTemperatureMax] [decimal](5, 2) NULL,
	[PersonalCareNotes] [nvarchar](2000) NULL,
	[ActualPlacement] [int] NULL,
	[ActualLight] [int] NULL,
	[HasRainCover] [bit] NULL,
	[SubstrateType] [nvarchar](max) NULL,
	[SaucerState] [int] NULL,
	[City] [nvarchar](max) NULL,
	[OverrideSuggestedLight] [int] NULL,
	[OverrideCareTaboosJson] [nvarchar](max) NULL,
	[WateringIntervalDetachedFromWiki] [bit] NOT NULL,
	[EnvironmentMismatchAcknowledged] [bit] NOT NULL,
	[AiEnvironmentAdvice] [nvarchar](max) NULL,
 CONSTRAINT [PK_PlantProfile_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantProfile] ADD  CONSTRAINT [DF_PlantProfile_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantProfile] ADD  CONSTRAINT [DF_PlantProfile_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantProfile] ADD  CONSTRAINT [DF_PlantProfile_WaterDetached_New]  DEFAULT ((0)) FOR [WateringIntervalDetachedFromWiki]
GO

ALTER TABLE [dbo].[PlantProfile] ADD  CONSTRAINT [DF_PlantProfile_EnvAck_New]  DEFAULT ((0)) FOR [EnvironmentMismatchAcknowledged]
GO

ALTER TABLE [dbo].[PlantProfile]  WITH CHECK ADD  CONSTRAINT [FK_PlantProfile_Plant_New] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantProfile] CHECK CONSTRAINT [FK_PlantProfile_Plant_New]
GO


