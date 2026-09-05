/*
================================================================================
  MidnightECPlant — 001_rebuild_plant_schema.sql
================================================================================
  目的：
    依標準六欄重建全部業務表，匯入既有資料後刪除舊表。

  標準欄（每表）：
    ID          uniqueidentifier  PK（NEWID）
    Seq         int IDENTITY(1,1) 人類可識別流水（匯入時 Seq = 舊 int Id）
    CreateDate  datetime2
    ModifyDate  datetime2
    Enabled     bit
    Deleted     bit

  定案對照（grill）：
    PK=Guid；URL 用 ID；Seq=IDENTITY；軟刪 Enabled=1 AND Deleted=0；
    表名單數；Enum 仍 int；NickName 在 Enabled=1 AND Deleted=0 唯一。

  執行前：
    1. 備份資料庫 MidnightECPlant
    2. 確認無應用程式連線寫入
    3. 整份在交易內執行；驗證失敗會 THROW → 整批 ROLLBACK

  注意：
    約束／索引名稱帶 _New，避免與舊表物件撞名；表 rename 後可保留此名。
================================================================================
*/

USE [MidnightECPlant];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

--------------------------------------------------------------------------------
-- 0) 防護：若 *_New 已存在則中止
--------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Plant_New', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.PlantSpecies_New', N'U') IS NOT NULL
BEGIN
    THROW 50001, N'偵測到 *_New 表已存在，請先清理後再執行本腳本。', 1;
END;

--------------------------------------------------------------------------------
-- 1) Guid 對照表（舊 int Id → 新 Guid）
--------------------------------------------------------------------------------
CREATE TABLE dbo._Map_PlantSpecies (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantKnowledge (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_Plant (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantProfile (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantDiary (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantImage (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantCareRecord (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantReminder (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantAnalysis (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantAnalysisJob (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantSource (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantSourceContent (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);
CREATE TABLE dbo._Map_PlantKnowledgeSyncLog (
    OldId int NOT NULL PRIMARY KEY,
    NewId uniqueidentifier NOT NULL UNIQUE
);

INSERT INTO dbo._Map_PlantSpecies (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantSpecies;

INSERT INTO dbo._Map_PlantKnowledge (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantKnowledge;

INSERT INTO dbo._Map_Plant (OldId, NewId)
SELECT Id, NEWID() FROM dbo.Plants;

INSERT INTO dbo._Map_PlantProfile (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantProfiles;

INSERT INTO dbo._Map_PlantDiary (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantDiaries;

INSERT INTO dbo._Map_PlantImage (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantImages;

INSERT INTO dbo._Map_PlantCareRecord (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantCareRecords;

INSERT INTO dbo._Map_PlantReminder (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantReminders;

INSERT INTO dbo._Map_PlantAnalysis (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantAnalyses;

INSERT INTO dbo._Map_PlantAnalysisJob (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantAnalysisJobs;

INSERT INTO dbo._Map_PlantSource (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantSources;

INSERT INTO dbo._Map_PlantSourceContent (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantSourceContents;

INSERT INTO dbo._Map_PlantKnowledgeSyncLog (OldId, NewId)
SELECT Id, NEWID() FROM dbo.PlantKnowledgeSyncLogs;

--------------------------------------------------------------------------------
-- 2) 建立新表（*_New）— 約束／索引皆帶 _New 後綴
--------------------------------------------------------------------------------

CREATE TABLE dbo.PlantSpecies_New (
    ID              uniqueidentifier NOT NULL CONSTRAINT PK_PlantSpecies_New PRIMARY KEY,
    Seq             int NOT NULL IDENTITY(1, 1),
    CreateDate      datetime2 NOT NULL,
    ModifyDate      datetime2 NOT NULL,
    Enabled         bit NOT NULL CONSTRAINT DF_PlantSpecies_Enabled_New DEFAULT (1),
    Deleted         bit NOT NULL CONSTRAINT DF_PlantSpecies_Deleted_New DEFAULT (0),
    ScientificName  nvarchar(256) NOT NULL,
    CommonName      nvarchar(256) NULL,
    ChineseName     nvarchar(256) NULL,
    Genus           nvarchar(128) NULL,
    Family          nvarchar(128) NULL,
    TaxonId         nvarchar(128) NULL,
    ImageUrl        nvarchar(1024) NULL,
    SourceType      nvarchar(64) NULL,
    SourceId        nvarchar(128) NULL
);
CREATE INDEX IX_PlantSpecies_ScientificName_New ON dbo.PlantSpecies_New (ScientificName);
CREATE INDEX IX_PlantSpecies_CommonName_New ON dbo.PlantSpecies_New (CommonName);
CREATE INDEX IX_PlantSpecies_ChineseName_New ON dbo.PlantSpecies_New (ChineseName);
CREATE INDEX IX_PlantSpecies_TaxonId_New ON dbo.PlantSpecies_New (TaxonId);
CREATE INDEX IX_PlantSpecies_Enabled_Deleted_New ON dbo.PlantSpecies_New (Enabled, Deleted);

CREATE TABLE dbo.PlantKnowledge_New (
    ID                              uniqueidentifier NOT NULL CONSTRAINT PK_PlantKnowledge_New PRIMARY KEY,
    Seq                             int NOT NULL IDENTITY(1, 1),
    CreateDate                      datetime2 NOT NULL,
    ModifyDate                      datetime2 NOT NULL,
    Enabled                         bit NOT NULL CONSTRAINT DF_PlantKnowledge_Enabled_New DEFAULT (1),
    Deleted                         bit NOT NULL CONSTRAINT DF_PlantKnowledge_Deleted_New DEFAULT (0),
    SpeciesID                       uniqueidentifier NOT NULL,
    LightRequirement                nvarchar(max) NULL,
    WaterRequirement                nvarchar(max) NULL,
    HumidityRequirement             nvarchar(max) NULL,
    TemperatureMin                  decimal(5, 2) NULL,
    TemperatureMax                  decimal(5, 2) NULL,
    SoilRequirement                 nvarchar(max) NULL,
    FertilizerRequirement           nvarchar(max) NULL,
    Dormancy                        nvarchar(max) NULL,
    GrowthSeason                    nvarchar(max) NULL,
    RepottingAdvice                 nvarchar(max) NULL,
    CommonProblems                  nvarchar(max) NULL,
    PestProblems                    nvarchar(max) NULL,
    DiseaseProblems                 nvarchar(max) NULL,
    CareSummary                     nvarchar(max) NULL,
    ExternalCareGuide               nvarchar(max) NULL,
    SuggestedLight                  int NULL,
    CareTaboosJson                  nvarchar(max) NULL,
    SuggestedWateringIntervalDays   int NULL,
    SourceUpdatedAt                 datetime2 NULL,
    DataVersion                     int NOT NULL CONSTRAINT DF_PlantKnowledge_DataVersion_New DEFAULT (1),
    CONSTRAINT FK_PlantKnowledge_Species_New FOREIGN KEY (SpeciesID) REFERENCES dbo.PlantSpecies_New (ID)
);
CREATE UNIQUE INDEX UX_PlantKnowledge_SpeciesID_New ON dbo.PlantKnowledge_New (SpeciesID) WHERE Deleted = 0;
CREATE INDEX IX_PlantKnowledge_Enabled_Deleted_New ON dbo.PlantKnowledge_New (Enabled, Deleted);

CREATE TABLE dbo.Plant_New (
    ID               uniqueidentifier NOT NULL CONSTRAINT PK_Plant_New PRIMARY KEY,
    Seq              int NOT NULL IDENTITY(1, 1),
    CreateDate       datetime2 NOT NULL,
    ModifyDate       datetime2 NOT NULL,
    Enabled          bit NOT NULL CONSTRAINT DF_Plant_Enabled_New DEFAULT (1),
    Deleted          bit NOT NULL CONSTRAINT DF_Plant_Deleted_New DEFAULT (0),
    Name             nvarchar(256) NOT NULL,
    SpeciesID        uniqueidentifier NOT NULL,
    NickName         nvarchar(256) NULL,
    Description      nvarchar(max) NULL,
    Location         nvarchar(256) NULL,
    EnvironmentNote  nvarchar(max) NULL,
    PurchaseDate     datetime2 NULL,
    StartDate        datetime2 NULL,
    CONSTRAINT FK_Plant_Species_New FOREIGN KEY (SpeciesID) REFERENCES dbo.PlantSpecies_New (ID)
);
CREATE INDEX IX_Plant_SpeciesID_New ON dbo.Plant_New (SpeciesID);
CREATE INDEX IX_Plant_Enabled_Deleted_New ON dbo.Plant_New (Enabled, Deleted);
CREATE UNIQUE INDEX UX_Plant_NickName_Active_New
    ON dbo.Plant_New (NickName)
    WHERE Deleted = 0 AND Enabled = 1 AND NickName IS NOT NULL;

CREATE TABLE dbo.PlantProfile_New (
    ID                               uniqueidentifier NOT NULL CONSTRAINT PK_PlantProfile_New PRIMARY KEY,
    Seq                              int NOT NULL IDENTITY(1, 1),
    CreateDate                       datetime2 NOT NULL,
    ModifyDate                       datetime2 NOT NULL,
    Enabled                          bit NOT NULL CONSTRAINT DF_PlantProfile_Enabled_New DEFAULT (1),
    Deleted                          bit NOT NULL CONSTRAINT DF_PlantProfile_Deleted_New DEFAULT (0),
    PlantID                          uniqueidentifier NOT NULL,
    WateringIntervalDays             int NULL,
    FertilizingIntervalDays          int NULL,
    TargetHumidityMin                decimal(5, 2) NULL,
    TargetHumidityMax                decimal(5, 2) NULL,
    TargetTemperatureMin             decimal(5, 2) NULL,
    TargetTemperatureMax             decimal(5, 2) NULL,
    PersonalCareNotes                nvarchar(2000) NULL,
    ActualPlacement                  int NULL,
    ActualLight                      int NULL,
    HasRainCover                     bit NULL,
    SubstrateType                    nvarchar(max) NULL,
    SaucerState                      int NULL,
    City                             nvarchar(max) NULL,
    OverrideSuggestedLight           int NULL,
    OverrideCareTaboosJson           nvarchar(max) NULL,
    WateringIntervalDetachedFromWiki bit NOT NULL CONSTRAINT DF_PlantProfile_WaterDetached_New DEFAULT (0),
    EnvironmentMismatchAcknowledged  bit NOT NULL CONSTRAINT DF_PlantProfile_EnvAck_New DEFAULT (0),
    AiEnvironmentAdvice              nvarchar(max) NULL,
    CONSTRAINT FK_PlantProfile_Plant_New FOREIGN KEY (PlantID) REFERENCES dbo.Plant_New (ID)
);
CREATE UNIQUE INDEX UX_PlantProfile_PlantID_New ON dbo.PlantProfile_New (PlantID) WHERE Deleted = 0;
CREATE INDEX IX_PlantProfile_Enabled_Deleted_New ON dbo.PlantProfile_New (Enabled, Deleted);

CREATE TABLE dbo.PlantDiary_New (
    ID               uniqueidentifier NOT NULL CONSTRAINT PK_PlantDiary_New PRIMARY KEY,
    Seq              int NOT NULL IDENTITY(1, 1),
    CreateDate       datetime2 NOT NULL,
    ModifyDate       datetime2 NOT NULL,
    Enabled          bit NOT NULL CONSTRAINT DF_PlantDiary_Enabled_New DEFAULT (1),
    Deleted          bit NOT NULL CONSTRAINT DF_PlantDiary_Deleted_New DEFAULT (0),
    PlantID          uniqueidentifier NOT NULL,
    DiaryDate        datetime2 NOT NULL,
    Title            nvarchar(256) NULL,
    Note             nvarchar(max) NULL,
    WeatherNote      nvarchar(max) NULL,
    EnvironmentNote  nvarchar(max) NULL,
    WateringNote     nvarchar(max) NULL,
    FertilizerNote   nvarchar(max) NULL,
    CONSTRAINT FK_PlantDiary_Plant_New FOREIGN KEY (PlantID) REFERENCES dbo.Plant_New (ID)
);
CREATE INDEX IX_PlantDiary_PlantID_DiaryDate_New ON dbo.PlantDiary_New (PlantID, DiaryDate);
CREATE INDEX IX_PlantDiary_Enabled_Deleted_New ON dbo.PlantDiary_New (Enabled, Deleted);

CREATE TABLE dbo.PlantImage_New (
    ID                uniqueidentifier NOT NULL CONSTRAINT PK_PlantImage_New PRIMARY KEY,
    Seq               int NOT NULL IDENTITY(1, 1),
    CreateDate        datetime2 NOT NULL,
    ModifyDate        datetime2 NOT NULL,
    Enabled           bit NOT NULL CONSTRAINT DF_PlantImage_Enabled_New DEFAULT (1),
    Deleted           bit NOT NULL CONSTRAINT DF_PlantImage_Deleted_New DEFAULT (0),
    PlantID           uniqueidentifier NOT NULL,
    DiaryID           uniqueidentifier NULL,
    Note              nvarchar(2000) NULL,
    IsCover           bit NOT NULL CONSTRAINT DF_PlantImage_IsCover_New DEFAULT (0),
    FileName          nvarchar(512) NOT NULL,
    StoragePath       nvarchar(1024) NOT NULL,
    ThumbnailPath     nvarchar(1024) NULL,
    OriginalFileName  nvarchar(512) NULL,
    ContentType       nvarchar(128) NULL,
    Width             int NULL,
    Height            int NULL,
    FileSize          bigint NOT NULL CONSTRAINT DF_PlantImage_FileSize_New DEFAULT (0),
    Sha256            nvarchar(64) NULL,
    CONSTRAINT FK_PlantImage_Plant_New FOREIGN KEY (PlantID) REFERENCES dbo.Plant_New (ID),
    CONSTRAINT FK_PlantImage_Diary_New FOREIGN KEY (DiaryID) REFERENCES dbo.PlantDiary_New (ID)
);
CREATE INDEX IX_PlantImage_PlantID_New ON dbo.PlantImage_New (PlantID);
CREATE INDEX IX_PlantImage_PlantID_IsCover_New ON dbo.PlantImage_New (PlantID, IsCover);
CREATE INDEX IX_PlantImage_DiaryID_New ON dbo.PlantImage_New (DiaryID);
CREATE INDEX IX_PlantImage_Enabled_Deleted_New ON dbo.PlantImage_New (Enabled, Deleted);

CREATE TABLE dbo.PlantCareRecord_New (
    ID            uniqueidentifier NOT NULL CONSTRAINT PK_PlantCareRecord_New PRIMARY KEY,
    Seq           int NOT NULL IDENTITY(1, 1),
    CreateDate    datetime2 NOT NULL,
    ModifyDate    datetime2 NOT NULL,
    Enabled       bit NOT NULL CONSTRAINT DF_PlantCareRecord_Enabled_New DEFAULT (1),
    Deleted       bit NOT NULL CONSTRAINT DF_PlantCareRecord_Deleted_New DEFAULT (0),
    PlantID       uniqueidentifier NOT NULL,
    RecordDate    datetime2 NOT NULL,
    CareType      int NOT NULL,
    NumericValue  decimal(10, 2) NULL,
    Unit          nvarchar(16) NULL,
    Note          nvarchar(max) NULL,
    CONSTRAINT FK_PlantCareRecord_Plant_New FOREIGN KEY (PlantID) REFERENCES dbo.Plant_New (ID)
);
CREATE INDEX IX_PlantCareRecord_PlantID_RecordDate_New ON dbo.PlantCareRecord_New (PlantID, RecordDate);
CREATE INDEX IX_PlantCareRecord_CareType_New ON dbo.PlantCareRecord_New (CareType);
CREATE INDEX IX_PlantCareRecord_Enabled_Deleted_New ON dbo.PlantCareRecord_New (Enabled, Deleted);

CREATE TABLE dbo.PlantReminder_New (
    ID            uniqueidentifier NOT NULL CONSTRAINT PK_PlantReminder_New PRIMARY KEY,
    Seq           int NOT NULL IDENTITY(1, 1),
    CreateDate    datetime2 NOT NULL,
    ModifyDate    datetime2 NOT NULL,
    Enabled       bit NOT NULL CONSTRAINT DF_PlantReminder_Enabled_New DEFAULT (1),
    Deleted       bit NOT NULL CONSTRAINT DF_PlantReminder_Deleted_New DEFAULT (0),
    PlantID       uniqueidentifier NOT NULL,
    ReminderType  int NOT NULL,
    Priority      int NOT NULL,
    Status        int NOT NULL,
    Title         nvarchar(256) NOT NULL,
    Message       nvarchar(2000) NULL,
    DueDate       datetime2 NOT NULL,
    SourceKey     nvarchar(128) NOT NULL,
    DismissedAt   datetime2 NULL,
    CONSTRAINT FK_PlantReminder_Plant_New FOREIGN KEY (PlantID) REFERENCES dbo.Plant_New (ID)
);
CREATE UNIQUE INDEX UX_PlantReminder_PlantID_SourceKey_New
    ON dbo.PlantReminder_New (PlantID, SourceKey) WHERE Deleted = 0;
CREATE INDEX IX_PlantReminder_PlantID_Status_New ON dbo.PlantReminder_New (PlantID, Status);
CREATE INDEX IX_PlantReminder_DueDate_New ON dbo.PlantReminder_New (DueDate);
CREATE INDEX IX_PlantReminder_Enabled_Deleted_New ON dbo.PlantReminder_New (Enabled, Deleted);

CREATE TABLE dbo.PlantAnalysis_New (
    ID              uniqueidentifier NOT NULL CONSTRAINT PK_PlantAnalysis_New PRIMARY KEY,
    Seq             int NOT NULL IDENTITY(1, 1),
    CreateDate      datetime2 NOT NULL,
    ModifyDate      datetime2 NOT NULL,
    Enabled         bit NOT NULL CONSTRAINT DF_PlantAnalysis_Enabled_New DEFAULT (1),
    Deleted         bit NOT NULL CONSTRAINT DF_PlantAnalysis_Deleted_New DEFAULT (0),
    PlantID         uniqueidentifier NOT NULL,
    DiaryID         uniqueidentifier NULL,
    ImageID         uniqueidentifier NULL,
    AnalysisType    int NOT NULL,
    AnalysisScope   int NOT NULL,
    ModelName       nvarchar(128) NULL,
    PromptVersion   nvarchar(64) NULL,
    InputSnapshot   nvarchar(max) NULL,
    ResultJson      nvarchar(max) NULL,
    Summary         nvarchar(2000) NULL,
    HealthScore     int NULL,
    Confidence      decimal(5, 4) NULL,
    CONSTRAINT FK_PlantAnalysis_Plant_New FOREIGN KEY (PlantID) REFERENCES dbo.Plant_New (ID),
    CONSTRAINT FK_PlantAnalysis_Diary_New FOREIGN KEY (DiaryID) REFERENCES dbo.PlantDiary_New (ID),
    CONSTRAINT FK_PlantAnalysis_Image_New FOREIGN KEY (ImageID) REFERENCES dbo.PlantImage_New (ID)
);
CREATE INDEX IX_PlantAnalysis_PlantID_CreateDate_New ON dbo.PlantAnalysis_New (PlantID, CreateDate);
CREATE INDEX IX_PlantAnalysis_DiaryID_New ON dbo.PlantAnalysis_New (DiaryID);
CREATE INDEX IX_PlantAnalysis_Enabled_Deleted_New ON dbo.PlantAnalysis_New (Enabled, Deleted);

CREATE TABLE dbo.PlantAnalysisJob_New (
    ID              uniqueidentifier NOT NULL CONSTRAINT PK_PlantAnalysisJob_New PRIMARY KEY,
    Seq             int NOT NULL IDENTITY(1, 1),
    CreateDate      datetime2 NOT NULL,
    ModifyDate      datetime2 NOT NULL,
    Enabled         bit NOT NULL CONSTRAINT DF_PlantAnalysisJob_Enabled_New DEFAULT (1),
    Deleted         bit NOT NULL CONSTRAINT DF_PlantAnalysisJob_Deleted_New DEFAULT (0),
    PlantID         uniqueidentifier NOT NULL,
    DiaryID         uniqueidentifier NULL,
    ImageID         uniqueidentifier NULL,
    AnalysisID      uniqueidentifier NULL,
    Status          int NOT NULL,
    AnalysisScope   int NOT NULL,
    RetryCount      int NOT NULL CONSTRAINT DF_PlantAnalysisJob_Retry_New DEFAULT (0),
    StartedAt       datetime2 NULL,
    CompletedAt     datetime2 NULL,
    ErrorMessage    nvarchar(max) NULL,
    CONSTRAINT FK_PlantAnalysisJob_Plant_New FOREIGN KEY (PlantID) REFERENCES dbo.Plant_New (ID),
    CONSTRAINT FK_PlantAnalysisJob_Diary_New FOREIGN KEY (DiaryID) REFERENCES dbo.PlantDiary_New (ID),
    CONSTRAINT FK_PlantAnalysisJob_Image_New FOREIGN KEY (ImageID) REFERENCES dbo.PlantImage_New (ID),
    CONSTRAINT FK_PlantAnalysisJob_Analysis_New FOREIGN KEY (AnalysisID) REFERENCES dbo.PlantAnalysis_New (ID)
);
CREATE UNIQUE INDEX UX_PlantAnalysisJob_AnalysisID_New
    ON dbo.PlantAnalysisJob_New (AnalysisID) WHERE AnalysisID IS NOT NULL AND Deleted = 0;
CREATE INDEX IX_PlantAnalysisJob_PlantID_New ON dbo.PlantAnalysisJob_New (PlantID);
CREATE INDEX IX_PlantAnalysisJob_Status_New ON dbo.PlantAnalysisJob_New (Status);
CREATE INDEX IX_PlantAnalysisJob_Enabled_Deleted_New ON dbo.PlantAnalysisJob_New (Enabled, Deleted);

CREATE TABLE dbo.PlantSource_New (
    ID                 uniqueidentifier NOT NULL CONSTRAINT PK_PlantSource_New PRIMARY KEY,
    Seq                int NOT NULL IDENTITY(1, 1),
    CreateDate         datetime2 NOT NULL,
    ModifyDate         datetime2 NOT NULL,
    Enabled            bit NOT NULL CONSTRAINT DF_PlantSource_Enabled_New DEFAULT (1),
    Deleted            bit NOT NULL CONSTRAINT DF_PlantSource_Deleted_New DEFAULT (0),
    SpeciesID          uniqueidentifier NOT NULL,
    SourceType         int NOT NULL,
    Title              nvarchar(512) NULL,
    Url                nvarchar(2048) NOT NULL,
    Domain             nvarchar(256) NULL,
    Author             nvarchar(256) NULL,
    PublishedAt        datetime2 NULL,
    Language           nvarchar(16) NULL,
    ContentHash        nvarchar(64) NULL,
    ReliabilityLevel   int NOT NULL CONSTRAINT DF_PlantSource_Reliability_New DEFAULT (3),
    CONSTRAINT FK_PlantSource_Species_New FOREIGN KEY (SpeciesID) REFERENCES dbo.PlantSpecies_New (ID)
);
CREATE INDEX IX_PlantSource_SpeciesID_New ON dbo.PlantSource_New (SpeciesID);
CREATE INDEX IX_PlantSource_Url_New ON dbo.PlantSource_New (Url);
CREATE INDEX IX_PlantSource_ContentHash_New ON dbo.PlantSource_New (ContentHash);
CREATE INDEX IX_PlantSource_Enabled_Deleted_New ON dbo.PlantSource_New (Enabled, Deleted);

CREATE TABLE dbo.PlantSourceContent_New (
    ID              uniqueidentifier NOT NULL CONSTRAINT PK_PlantSourceContent_New PRIMARY KEY,
    Seq             int NOT NULL IDENTITY(1, 1),
    CreateDate      datetime2 NOT NULL,
    ModifyDate      datetime2 NOT NULL,
    Enabled         bit NOT NULL CONSTRAINT DF_PlantSourceContent_Enabled_New DEFAULT (1),
    Deleted         bit NOT NULL CONSTRAINT DF_PlantSourceContent_Deleted_New DEFAULT (0),
    SourceID        uniqueidentifier NOT NULL,
    RawText         nvarchar(max) NULL,
    CleanText       nvarchar(max) NULL,
    Summary         nvarchar(max) NULL,
    Keywords        nvarchar(max) NULL,
    ParsedJson      nvarchar(max) NULL,
    ParserType      nvarchar(128) NULL,
    ParserVersion   nvarchar(64) NULL,
    ContentHash     nvarchar(64) NULL,
    Status          int NOT NULL,
    ErrorMessage    nvarchar(max) NULL,
    ParsedAt        datetime2 NULL,
    CONSTRAINT FK_PlantSourceContent_Source_New FOREIGN KEY (SourceID) REFERENCES dbo.PlantSource_New (ID)
);
CREATE INDEX IX_PlantSourceContent_SourceID_New ON dbo.PlantSourceContent_New (SourceID);
CREATE INDEX IX_PlantSourceContent_ContentHash_New ON dbo.PlantSourceContent_New (ContentHash);
CREATE INDEX IX_PlantSourceContent_Enabled_Deleted_New ON dbo.PlantSourceContent_New (Enabled, Deleted);

CREATE TABLE dbo.PlantSourceSpecies_New (
    ID          uniqueidentifier NOT NULL CONSTRAINT PK_PlantSourceSpecies_New PRIMARY KEY,
    Seq         int NOT NULL IDENTITY(1, 1),
    CreateDate  datetime2 NOT NULL,
    ModifyDate  datetime2 NOT NULL,
    Enabled     bit NOT NULL CONSTRAINT DF_PlantSourceSpecies_Enabled_New DEFAULT (1),
    Deleted     bit NOT NULL CONSTRAINT DF_PlantSourceSpecies_Deleted_New DEFAULT (0),
    SourceID    uniqueidentifier NOT NULL,
    SpeciesID   uniqueidentifier NOT NULL,
    CONSTRAINT FK_PlantSourceSpecies_Source_New FOREIGN KEY (SourceID) REFERENCES dbo.PlantSource_New (ID),
    CONSTRAINT FK_PlantSourceSpecies_Species_New FOREIGN KEY (SpeciesID) REFERENCES dbo.PlantSpecies_New (ID)
);
CREATE UNIQUE INDEX UX_PlantSourceSpecies_Source_Species_New
    ON dbo.PlantSourceSpecies_New (SourceID, SpeciesID) WHERE Deleted = 0;
CREATE INDEX IX_PlantSourceSpecies_SpeciesID_New ON dbo.PlantSourceSpecies_New (SpeciesID);
CREATE INDEX IX_PlantSourceSpecies_Enabled_Deleted_New ON dbo.PlantSourceSpecies_New (Enabled, Deleted);

CREATE TABLE dbo.PlantKnowledgeSyncLog_New (
    ID            uniqueidentifier NOT NULL CONSTRAINT PK_PlantKnowledgeSyncLog_New PRIMARY KEY,
    Seq           int NOT NULL IDENTITY(1, 1),
    CreateDate    datetime2 NOT NULL,
    ModifyDate    datetime2 NOT NULL,
    Enabled       bit NOT NULL CONSTRAINT DF_PlantKnowledgeSyncLog_Enabled_New DEFAULT (1),
    Deleted       bit NOT NULL CONSTRAINT DF_PlantKnowledgeSyncLog_Deleted_New DEFAULT (0),
    SpeciesID     uniqueidentifier NOT NULL,
    Provider      int NOT NULL,
    RequestUrl    nvarchar(2048) NULL,
    Status        int NOT NULL,
    ResponseHash  nvarchar(64) NULL,
    StartedAt     datetime2 NOT NULL,
    CompletedAt   datetime2 NULL,
    ErrorMessage  nvarchar(max) NULL,
    CONSTRAINT FK_PlantKnowledgeSyncLog_Species_New FOREIGN KEY (SpeciesID) REFERENCES dbo.PlantSpecies_New (ID)
);
CREATE INDEX IX_PlantKnowledgeSyncLog_SpeciesID_New ON dbo.PlantKnowledgeSyncLog_New (SpeciesID);
CREATE INDEX IX_PlantKnowledgeSyncLog_Provider_New ON dbo.PlantKnowledgeSyncLog_New (Provider);
CREATE INDEX IX_PlantKnowledgeSyncLog_Enabled_Deleted_New ON dbo.PlantKnowledgeSyncLog_New (Enabled, Deleted);

--------------------------------------------------------------------------------
-- 3) 匯入（IDENTITY_INSERT：Seq = 舊 int Id）
--------------------------------------------------------------------------------

SET IDENTITY_INSERT dbo.PlantSpecies_New ON;
INSERT INTO dbo.PlantSpecies_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted,
    ScientificName, CommonName, ChineseName, Genus, Family, TaxonId, ImageUrl, SourceType, SourceId
)
SELECT
    m.NewId, s.Id, s.CreatedAt, s.UpdatedAt, 1, 0,
    s.ScientificName, s.CommonName, s.ChineseName, s.Genus, s.Family, s.TaxonId, s.ImageUrl, s.SourceType, s.SourceId
FROM dbo.PlantSpecies s
INNER JOIN dbo._Map_PlantSpecies m ON m.OldId = s.Id
ORDER BY s.Id;
SET IDENTITY_INSERT dbo.PlantSpecies_New OFF;

SET IDENTITY_INSERT dbo.PlantKnowledge_New ON;
INSERT INTO dbo.PlantKnowledge_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID,
    LightRequirement, WaterRequirement, HumidityRequirement, TemperatureMin, TemperatureMax,
    SoilRequirement, FertilizerRequirement, Dormancy, GrowthSeason, RepottingAdvice,
    CommonProblems, PestProblems, DiseaseProblems, CareSummary, ExternalCareGuide,
    SuggestedLight, CareTaboosJson, SuggestedWateringIntervalDays, SourceUpdatedAt, DataVersion
)
SELECT
    m.NewId, k.Id, k.UpdatedAt, k.UpdatedAt, 1, 0, ms.NewId,
    k.LightRequirement, k.WaterRequirement, k.HumidityRequirement, k.TemperatureMin, k.TemperatureMax,
    k.SoilRequirement, k.FertilizerRequirement, k.Dormancy, k.GrowthSeason, k.RepottingAdvice,
    k.CommonProblems, k.PestProblems, k.DiseaseProblems, k.CareSummary, k.ExternalCareGuide,
    k.SuggestedLight, k.CareTaboosJson, k.SuggestedWateringIntervalDays, k.SourceUpdatedAt, k.DataVersion
FROM dbo.PlantKnowledge k
INNER JOIN dbo._Map_PlantKnowledge m ON m.OldId = k.Id
INNER JOIN dbo._Map_PlantSpecies ms ON ms.OldId = k.SpeciesId
ORDER BY k.Id;
SET IDENTITY_INSERT dbo.PlantKnowledge_New OFF;

SET IDENTITY_INSERT dbo.Plant_New ON;
INSERT INTO dbo.Plant_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted,
    Name, SpeciesID, NickName, Description, Location, EnvironmentNote, PurchaseDate, StartDate
)
SELECT
    m.NewId, p.Id, p.CreatedAt, p.UpdatedAt, p.IsActive, 0,
    p.Name, ms.NewId, p.NickName, p.Description, p.Location, p.EnvironmentNote, p.PurchaseDate, p.StartDate
FROM dbo.Plants p
INNER JOIN dbo._Map_Plant m ON m.OldId = p.Id
INNER JOIN dbo._Map_PlantSpecies ms ON ms.OldId = p.SpeciesId
ORDER BY p.Id;
SET IDENTITY_INSERT dbo.Plant_New OFF;

SET IDENTITY_INSERT dbo.PlantProfile_New ON;
INSERT INTO dbo.PlantProfile_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
    WateringIntervalDays, FertilizingIntervalDays,
    TargetHumidityMin, TargetHumidityMax, TargetTemperatureMin, TargetTemperatureMax,
    PersonalCareNotes, ActualPlacement, ActualLight, HasRainCover, SubstrateType, SaucerState, City,
    OverrideSuggestedLight, OverrideCareTaboosJson,
    WateringIntervalDetachedFromWiki, EnvironmentMismatchAcknowledged, AiEnvironmentAdvice
)
SELECT
    m.NewId, pr.Id, pr.CreatedAt, pr.UpdatedAt, 1, 0, mp.NewId,
    pr.WateringIntervalDays, pr.FertilizingIntervalDays,
    pr.TargetHumidityMin, pr.TargetHumidityMax, pr.TargetTemperatureMin, pr.TargetTemperatureMax,
    pr.PersonalCareNotes, pr.ActualPlacement, pr.ActualLight, pr.HasRainCover, pr.SubstrateType, pr.SaucerState, pr.City,
    pr.OverrideSuggestedLight, pr.OverrideCareTaboosJson,
    pr.WateringIntervalDetachedFromWiki, pr.EnvironmentMismatchAcknowledged, pr.AiEnvironmentAdvice
FROM dbo.PlantProfiles pr
INNER JOIN dbo._Map_PlantProfile m ON m.OldId = pr.Id
INNER JOIN dbo._Map_Plant mp ON mp.OldId = pr.PlantId
ORDER BY pr.Id;
SET IDENTITY_INSERT dbo.PlantProfile_New OFF;

SET IDENTITY_INSERT dbo.PlantDiary_New ON;
INSERT INTO dbo.PlantDiary_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
    DiaryDate, Title, Note, WeatherNote, EnvironmentNote, WateringNote, FertilizerNote
)
SELECT
    m.NewId, d.Id, d.CreatedAt, d.UpdatedAt, 1, 0, mp.NewId,
    d.DiaryDate, d.Title, d.Note, d.WeatherNote, d.EnvironmentNote, d.WateringNote, d.FertilizerNote
FROM dbo.PlantDiaries d
INNER JOIN dbo._Map_PlantDiary m ON m.OldId = d.Id
INNER JOIN dbo._Map_Plant mp ON mp.OldId = d.PlantId
ORDER BY d.Id;
SET IDENTITY_INSERT dbo.PlantDiary_New OFF;

SET IDENTITY_INSERT dbo.PlantImage_New ON;
INSERT INTO dbo.PlantImage_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID,
    Note, IsCover, FileName, StoragePath, ThumbnailPath, OriginalFileName, ContentType, Width, Height, FileSize, Sha256
)
SELECT
    m.NewId, i.Id, i.CreatedAt, i.CreatedAt, 1, 0, mp.NewId, md.NewId,
    i.Note, i.IsCover, i.FileName, i.StoragePath, i.ThumbnailPath, i.OriginalFileName, i.ContentType, i.Width, i.Height, i.FileSize, i.Sha256
FROM dbo.PlantImages i
INNER JOIN dbo._Map_PlantImage m ON m.OldId = i.Id
INNER JOIN dbo._Map_Plant mp ON mp.OldId = i.PlantId
LEFT JOIN dbo._Map_PlantDiary md ON md.OldId = i.DiaryId
ORDER BY i.Id;
SET IDENTITY_INSERT dbo.PlantImage_New OFF;

SET IDENTITY_INSERT dbo.PlantCareRecord_New ON;
INSERT INTO dbo.PlantCareRecord_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
    RecordDate, CareType, NumericValue, Unit, Note
)
SELECT
    m.NewId, c.Id, c.CreatedAt, c.CreatedAt, 1, 0, mp.NewId,
    c.RecordDate, c.CareType, c.NumericValue, c.Unit, c.Note
FROM dbo.PlantCareRecords c
INNER JOIN dbo._Map_PlantCareRecord m ON m.OldId = c.Id
INNER JOIN dbo._Map_Plant mp ON mp.OldId = c.PlantId
ORDER BY c.Id;
SET IDENTITY_INSERT dbo.PlantCareRecord_New OFF;

SET IDENTITY_INSERT dbo.PlantReminder_New ON;
INSERT INTO dbo.PlantReminder_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
    ReminderType, Priority, Status, Title, Message, DueDate, SourceKey, DismissedAt
)
SELECT
    m.NewId, r.Id, r.CreatedAt, r.UpdatedAt, 1, 0, mp.NewId,
    r.ReminderType, r.Priority, r.Status, r.Title, r.Message, r.DueDate, r.SourceKey, r.DismissedAt
FROM dbo.PlantReminders r
INNER JOIN dbo._Map_PlantReminder m ON m.OldId = r.Id
INNER JOIN dbo._Map_Plant mp ON mp.OldId = r.PlantId
ORDER BY r.Id;
SET IDENTITY_INSERT dbo.PlantReminder_New OFF;

SET IDENTITY_INSERT dbo.PlantAnalysis_New ON;
INSERT INTO dbo.PlantAnalysis_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID, ImageID,
    AnalysisType, AnalysisScope, ModelName, PromptVersion, InputSnapshot, ResultJson, Summary, HealthScore, Confidence
)
SELECT
    m.NewId, a.Id, a.CreatedAt, a.CreatedAt, 1, 0, mp.NewId, md.NewId, mi.NewId,
    a.AnalysisType, a.AnalysisScope, a.ModelName, a.PromptVersion, a.InputSnapshot, a.ResultJson, a.Summary, a.HealthScore, a.Confidence
FROM dbo.PlantAnalyses a
INNER JOIN dbo._Map_PlantAnalysis m ON m.OldId = a.Id
INNER JOIN dbo._Map_Plant mp ON mp.OldId = a.PlantId
LEFT JOIN dbo._Map_PlantDiary md ON md.OldId = a.DiaryId
LEFT JOIN dbo._Map_PlantImage mi ON mi.OldId = a.ImageId
ORDER BY a.Id;
SET IDENTITY_INSERT dbo.PlantAnalysis_New OFF;

SET IDENTITY_INSERT dbo.PlantAnalysisJob_New ON;
INSERT INTO dbo.PlantAnalysisJob_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID, ImageID, AnalysisID,
    Status, AnalysisScope, RetryCount, StartedAt, CompletedAt, ErrorMessage
)
SELECT
    m.NewId, j.Id, j.CreatedAt, ISNULL(j.CompletedAt, ISNULL(j.StartedAt, j.CreatedAt)), 1, 0,
    mp.NewId, md.NewId, mi.NewId, ma.NewId,
    j.Status, j.AnalysisScope, j.RetryCount, j.StartedAt, j.CompletedAt, j.ErrorMessage
FROM dbo.PlantAnalysisJobs j
INNER JOIN dbo._Map_PlantAnalysisJob m ON m.OldId = j.Id
INNER JOIN dbo._Map_Plant mp ON mp.OldId = j.PlantId
LEFT JOIN dbo._Map_PlantDiary md ON md.OldId = j.DiaryId
LEFT JOIN dbo._Map_PlantImage mi ON mi.OldId = j.ImageId
LEFT JOIN dbo._Map_PlantAnalysis ma ON ma.OldId = j.AnalysisId
ORDER BY j.Id;
SET IDENTITY_INSERT dbo.PlantAnalysisJob_New OFF;

SET IDENTITY_INSERT dbo.PlantSource_New ON;
INSERT INTO dbo.PlantSource_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID,
    SourceType, Title, Url, Domain, Author, PublishedAt, Language, ContentHash, ReliabilityLevel
)
SELECT
    m.NewId, s.Id, s.CreatedAt, s.UpdatedAt, s.IsActive, 0, ms.NewId,
    s.SourceType, s.Title, s.Url, s.Domain, s.Author, s.PublishedAt, s.Language, s.ContentHash, s.ReliabilityLevel
FROM dbo.PlantSources s
INNER JOIN dbo._Map_PlantSource m ON m.OldId = s.Id
INNER JOIN dbo._Map_PlantSpecies ms ON ms.OldId = s.SpeciesId
ORDER BY s.Id;
SET IDENTITY_INSERT dbo.PlantSource_New OFF;

SET IDENTITY_INSERT dbo.PlantSourceContent_New ON;
INSERT INTO dbo.PlantSourceContent_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SourceID,
    RawText, CleanText, Summary, Keywords, ParsedJson, ParserType, ParserVersion, ContentHash, Status, ErrorMessage, ParsedAt
)
SELECT
    m.NewId, c.Id, c.UpdatedAt, c.UpdatedAt, 1, 0, ms.NewId,
    c.RawText, c.CleanText, c.Summary, c.Keywords, c.ParsedJson, c.ParserType, c.ParserVersion, c.ContentHash, c.Status, c.ErrorMessage, c.ParsedAt
FROM dbo.PlantSourceContents c
INNER JOIN dbo._Map_PlantSourceContent m ON m.OldId = c.Id
INNER JOIN dbo._Map_PlantSource ms ON ms.OldId = c.SourceId
ORDER BY c.Id;
SET IDENTITY_INSERT dbo.PlantSourceContent_New OFF;

INSERT INTO dbo.PlantSourceSpecies_New (
    ID, CreateDate, ModifyDate, Enabled, Deleted, SourceID, SpeciesID
)
SELECT
    NEWID(), SYSUTCDATETIME(), SYSUTCDATETIME(), 1, 0, msrc.NewId, msp.NewId
FROM dbo.PlantSourceSpecies ss
INNER JOIN dbo._Map_PlantSource msrc ON msrc.OldId = ss.SourceId
INNER JOIN dbo._Map_PlantSpecies msp ON msp.OldId = ss.SpeciesId;

SET IDENTITY_INSERT dbo.PlantKnowledgeSyncLog_New ON;
INSERT INTO dbo.PlantKnowledgeSyncLog_New (
    ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID,
    Provider, RequestUrl, Status, ResponseHash, StartedAt, CompletedAt, ErrorMessage
)
SELECT
    m.NewId, l.Id, l.StartedAt, ISNULL(l.CompletedAt, l.StartedAt), 1, 0, ms.NewId,
    l.Provider, l.RequestUrl, l.Status, l.ResponseHash, l.StartedAt, l.CompletedAt, l.ErrorMessage
FROM dbo.PlantKnowledgeSyncLogs l
INNER JOIN dbo._Map_PlantKnowledgeSyncLog m ON m.OldId = l.Id
INNER JOIN dbo._Map_PlantSpecies ms ON ms.OldId = l.SpeciesId
ORDER BY l.Id;
SET IDENTITY_INSERT dbo.PlantKnowledgeSyncLog_New OFF;

--------------------------------------------------------------------------------
-- 4) 驗證列數
--------------------------------------------------------------------------------
DECLARE @errors nvarchar(max) = N'';

IF (SELECT COUNT(*) FROM dbo.PlantSpecies) <> (SELECT COUNT(*) FROM dbo.PlantSpecies_New)
    SET @errors += N'PlantSpecies count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantKnowledge) <> (SELECT COUNT(*) FROM dbo.PlantKnowledge_New)
    SET @errors += N'PlantKnowledge count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.Plants) <> (SELECT COUNT(*) FROM dbo.Plant_New)
    SET @errors += N'Plant count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantProfiles) <> (SELECT COUNT(*) FROM dbo.PlantProfile_New)
    SET @errors += N'PlantProfile count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantDiaries) <> (SELECT COUNT(*) FROM dbo.PlantDiary_New)
    SET @errors += N'PlantDiary count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantImages) <> (SELECT COUNT(*) FROM dbo.PlantImage_New)
    SET @errors += N'PlantImage count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantCareRecords) <> (SELECT COUNT(*) FROM dbo.PlantCareRecord_New)
    SET @errors += N'PlantCareRecord count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantReminders) <> (SELECT COUNT(*) FROM dbo.PlantReminder_New)
    SET @errors += N'PlantReminder count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantAnalyses) <> (SELECT COUNT(*) FROM dbo.PlantAnalysis_New)
    SET @errors += N'PlantAnalysis count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantAnalysisJobs) <> (SELECT COUNT(*) FROM dbo.PlantAnalysisJob_New)
    SET @errors += N'PlantAnalysisJob count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantSources) <> (SELECT COUNT(*) FROM dbo.PlantSource_New)
    SET @errors += N'PlantSource count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantSourceContents) <> (SELECT COUNT(*) FROM dbo.PlantSourceContent_New)
    SET @errors += N'PlantSourceContent count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantSourceSpecies) <> (SELECT COUNT(*) FROM dbo.PlantSourceSpecies_New)
    SET @errors += N'PlantSourceSpecies count mismatch; ';
IF (SELECT COUNT(*) FROM dbo.PlantKnowledgeSyncLogs) <> (SELECT COUNT(*) FROM dbo.PlantKnowledgeSyncLog_New)
    SET @errors += N'PlantKnowledgeSyncLog count mismatch; ';

IF EXISTS (
    SELECT 1
    FROM dbo.Plants p
    INNER JOIN dbo._Map_Plant m ON m.OldId = p.Id
    INNER JOIN dbo._Map_PlantSpecies ms ON ms.OldId = p.SpeciesId
    LEFT JOIN dbo.Plant_New n ON n.ID = m.NewId AND n.SpeciesID = ms.NewId
    WHERE n.ID IS NULL
)
    SET @errors += N'Plant FK sample mismatch; ';

IF @errors <> N''
BEGIN
    SET @errors = N'驗證失敗：' + @errors;
    THROW 50002, @errors, 1;
END;

--------------------------------------------------------------------------------
-- 5) 刪舊表 → rename *_New → 正式名 → 刪 EF 履歷
--------------------------------------------------------------------------------
DROP TABLE IF EXISTS dbo.PlantAnalysisJobs;
DROP TABLE IF EXISTS dbo.PlantAnalyses;
DROP TABLE IF EXISTS dbo.PlantSourceContents;
DROP TABLE IF EXISTS dbo.PlantSourceSpecies;
DROP TABLE IF EXISTS dbo.PlantSources;
DROP TABLE IF EXISTS dbo.PlantKnowledgeSyncLogs;
DROP TABLE IF EXISTS dbo.PlantReminders;
DROP TABLE IF EXISTS dbo.PlantCareRecords;
DROP TABLE IF EXISTS dbo.PlantImages;
DROP TABLE IF EXISTS dbo.PlantDiaries;
DROP TABLE IF EXISTS dbo.PlantProfiles;
DROP TABLE IF EXISTS dbo.Plants;
DROP TABLE IF EXISTS dbo.PlantKnowledge;
DROP TABLE IF EXISTS dbo.PlantSpecies;

DROP TABLE IF EXISTS dbo.__EFMigrationsHistory;

EXEC sp_rename N'dbo.PlantSpecies_New', N'PlantSpecies';
EXEC sp_rename N'dbo.PlantKnowledge_New', N'PlantKnowledge';
EXEC sp_rename N'dbo.Plant_New', N'Plant';
EXEC sp_rename N'dbo.PlantProfile_New', N'PlantProfile';
EXEC sp_rename N'dbo.PlantDiary_New', N'PlantDiary';
EXEC sp_rename N'dbo.PlantImage_New', N'PlantImage';
EXEC sp_rename N'dbo.PlantCareRecord_New', N'PlantCareRecord';
EXEC sp_rename N'dbo.PlantReminder_New', N'PlantReminder';
EXEC sp_rename N'dbo.PlantAnalysis_New', N'PlantAnalysis';
EXEC sp_rename N'dbo.PlantAnalysisJob_New', N'PlantAnalysisJob';
EXEC sp_rename N'dbo.PlantSource_New', N'PlantSource';
EXEC sp_rename N'dbo.PlantSourceContent_New', N'PlantSourceContent';
EXEC sp_rename N'dbo.PlantSourceSpecies_New', N'PlantSourceSpecies';
EXEC sp_rename N'dbo.PlantKnowledgeSyncLog_New', N'PlantKnowledgeSyncLog';

--------------------------------------------------------------------------------
-- 6) 清理對照表（若要留 int→Guid 除錯，註解本段）
--------------------------------------------------------------------------------
DROP TABLE dbo._Map_PlantSpecies;
DROP TABLE dbo._Map_PlantKnowledge;
DROP TABLE dbo._Map_Plant;
DROP TABLE dbo._Map_PlantProfile;
DROP TABLE dbo._Map_PlantDiary;
DROP TABLE dbo._Map_PlantImage;
DROP TABLE dbo._Map_PlantCareRecord;
DROP TABLE dbo._Map_PlantReminder;
DROP TABLE dbo._Map_PlantAnalysis;
DROP TABLE dbo._Map_PlantAnalysisJob;
DROP TABLE dbo._Map_PlantSource;
DROP TABLE dbo._Map_PlantSourceContent;
DROP TABLE dbo._Map_PlantKnowledgeSyncLog;

COMMIT TRANSACTION;

PRINT N'001_rebuild_plant_schema 完成：新表已就位，舊表與 __EFMigrationsHistory 已刪除。';
GO

/*
  執行後抽樣：
  SELECT TOP 20 Seq, ID, NickName, Name, Enabled, Deleted FROM dbo.Plant ORDER BY Seq;
  SELECT COUNT(*) FROM dbo.Plant;
  SELECT COUNT(*) FROM dbo.PlantCareRecord;
*/
