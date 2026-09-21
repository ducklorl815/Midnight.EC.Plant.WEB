USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[Plant]    指令碼日期: 2026/9/5 下午 11:01:04 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plant](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[SpeciesID] [uniqueidentifier] NOT NULL,
	[NickName] [nvarchar](256) NULL,
	[Description] [nvarchar](max) NULL,
	[Location] [nvarchar](256) NULL,
	[EnvironmentNote] [nvarchar](max) NULL,
	[PurchaseDate] [datetime2](7) NULL,
	[StartDate] [datetime2](7) NULL,
 CONSTRAINT [PK_Plant_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Plant] ADD  CONSTRAINT [DF_Plant_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[Plant] ADD  CONSTRAINT [DF_Plant_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[Plant]  WITH CHECK ADD  CONSTRAINT [FK_Plant_Species_New] FOREIGN KEY([SpeciesID])
REFERENCES [dbo].[PlantSpecies] ([ID])
GO

ALTER TABLE [dbo].[Plant] CHECK CONSTRAINT [FK_Plant_Species_New]
GO


