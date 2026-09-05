USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantSourceContent]    指令碼日期: 2026/9/5 下午 11:04:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantSourceContent](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[SourceID] [uniqueidentifier] NOT NULL,
	[RawText] [nvarchar](max) NULL,
	[CleanText] [nvarchar](max) NULL,
	[Summary] [nvarchar](max) NULL,
	[Keywords] [nvarchar](max) NULL,
	[ParsedJson] [nvarchar](max) NULL,
	[ParserType] [nvarchar](128) NULL,[dbo].[PlantSourceSpecies]
	[ParserVersion] [nvarchar](64) NULL,
	[ContentHash] [nvarchar](64) NULL,
	[Status] [int] NOT NULL,
	[ErrorMessage] [nvarchar](max) NULL,
	[ParsedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_PlantSourceContent_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantSourceContent] ADD  CONSTRAINT [DF_PlantSourceContent_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantSourceContent] ADD  CONSTRAINT [DF_PlantSourceContent_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantSourceContent]  WITH CHECK ADD  CONSTRAINT [FK_PlantSourceContent_Source_New] FOREIGN KEY([SourceID])
REFERENCES [dbo].[PlantSource] ([ID])
GO

ALTER TABLE [dbo].[PlantSourceContent] CHECK CONSTRAINT [FK_PlantSourceContent_Source_New]
GO


