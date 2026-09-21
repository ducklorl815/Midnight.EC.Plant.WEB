USE [MidnightECPlant]
GO

/****** 物件:  Table [dbo].[PlantReminder]    指令碼日期: 2026/9/5 下午 11:04:17 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PlantReminder](
	[ID] [uniqueidentifier] NOT NULL,
	[Seq] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[ModifyDate] [datetime2](7) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[PlantID] [uniqueidentifier] NOT NULL,
	[ReminderType] [int] NOT NULL,
	[Priority] [int] NOT NULL,
	[Status] [int] NOT NULL,
	[Title] [nvarchar](256) NOT NULL,
	[Message] [nvarchar](2000) NULL,
	[DueDate] [datetime2](7) NOT NULL,
	[SourceKey] [nvarchar](128) NOT NULL,
	[DismissedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_PlantReminder_New] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PlantReminder] ADD  CONSTRAINT [DF_PlantReminder_Enabled_New]  DEFAULT ((1)) FOR [Enabled]
GO

ALTER TABLE [dbo].[PlantReminder] ADD  CONSTRAINT [DF_PlantReminder_Deleted_New]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[PlantReminder]  WITH CHECK ADD  CONSTRAINT [FK_PlantReminder_Plant_New] FOREIGN KEY([PlantID])
REFERENCES [dbo].[Plant] ([ID])
GO

ALTER TABLE [dbo].[PlantReminder] CHECK CONSTRAINT [FK_PlantReminder_Plant_New]
GO


