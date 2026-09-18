USE [MidnightECPlant]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantEffectImage](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[OriginalPhotoID] [uniqueidentifier] NOT NULL,
	[GeneratedImagePath] [nvarchar](1024) NOT NULL,
	[Style] [nvarchar](64) NOT NULL,
	[Layout] [nvarchar](64) NOT NULL,
	[ColorPalette] [nvarchar](128) NULL,
	[DecorationJson] [nvarchar](max) NULL,
	[PromptVersion] [nvarchar](64) NOT NULL,
	[PromptText] [nvarchar](max) NULL,
	[Status] [int] NOT NULL,
	[GenerationRequestId] [nvarchar](128) NULL,
	[ErrorMessage] [nvarchar](1000) NULL,
	[IsLatest] [bit] NOT NULL,
 CONSTRAINT [PK_PlantEffectImage] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantEffectImage] ADD CONSTRAINT [DF_PlantEffectImage_Enabled] DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantEffectImage] ADD CONSTRAINT [DF_PlantEffectImage_Deleted] DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantEffectImage] ADD CONSTRAINT [DF_PlantEffectImage_Status] DEFAULT ((0)) FOR [Status]
GO

ALTER TABLE [dbo].[PlantEffectImage] ADD CONSTRAINT [DF_PlantEffectImage_IsLatest] DEFAULT ((1)) FOR [IsLatest]
GO

ALTER TABLE [dbo].[PlantEffectImage] WITH CHECK ADD CONSTRAINT [FK_PlantEffectImage_Plant] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantEffectImage] CHECK CONSTRAINT [FK_PlantEffectImage_Plant]
GO

ALTER TABLE [dbo].[PlantEffectImage] WITH CHECK ADD CONSTRAINT [FK_PlantEffectImage_OriginalPhoto] FOREIGN KEY([OriginalPhotoID])
REFERENCES [dbo].[PlantImage] ([ID])
GO

ALTER TABLE [dbo].[PlantEffectImage] CHECK CONSTRAINT [FK_PlantEffectImage_OriginalPhoto]
GO

CREATE NONCLUSTERED INDEX [IX_PlantEffectImage_Photo_Latest]
ON [dbo].[PlantEffectImage] ([OriginalPhotoID], [IsLatest], [Deleted], [Status], [CreateDate] DESC)
GO

CREATE NONCLUSTERED INDEX [IX_PlantEffectImage_Plant]
ON [dbo].[PlantEffectImage] ([PlantID], [Deleted], [CreateDate] DESC)
GO
