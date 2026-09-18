using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Extensions;

public static class EntityMappingExtensions
{
    public static PlantDto ToDto(this PlantModel entity) => new()
    {
        Id = entity.ID,
        Name = entity.Name,
        SpeciesId = entity.SpeciesID,
        NickName = entity.NickName,
        Description = entity.Description,
        Location = entity.Location,
        EnvironmentNote = entity.EnvironmentNote,
        PurchaseDate = entity.PurchaseDate,
        StartDate = entity.StartDate,
        Enabled = entity.Enabled,
        CreateDate = entity.CreateDate,
        ModifyDate = entity.ModifyDate,
        Species = entity.Species?.ToDto(),
        Knowledge = entity.Species?.Knowledge?.ToDto()
    };

    public static PlantSpeciesDto ToDto(this PlantSpeciesModel entity) => new()
    {
        Id = entity.ID,
        ScientificName = entity.ScientificName,
        CommonName = entity.CommonName,
        ChineseName = entity.ChineseName,
        Genus = entity.Genus,
        Family = entity.Family,
        TaxonId = entity.TaxonId,
        ImageUrl = entity.ImageUrl
    };

    public static PlantKnowledgeDto ToDto(this PlantKnowledgeModel entity) => new()
    {
        Id = entity.ID,
        SpeciesId = entity.SpeciesID,
        LightRequirement = entity.LightRequirement,
        WaterRequirement = entity.WaterRequirement,
        HumidityRequirement = entity.HumidityRequirement,
        TemperatureMin = entity.TemperatureMin,
        TemperatureMax = entity.TemperatureMax,
        SoilRequirement = entity.SoilRequirement,
        FertilizerRequirement = entity.FertilizerRequirement,
        Dormancy = entity.Dormancy,
        GrowthSeason = entity.GrowthSeason,
        RepottingAdvice = entity.RepottingAdvice,
        CommonProblems = entity.CommonProblems,
        PestProblems = entity.PestProblems,
        DiseaseProblems = entity.DiseaseProblems,
        CareSummary = entity.CareSummary,
        ExternalCareGuide = entity.ExternalCareGuide,
        SuggestedLight = entity.SuggestedLight,
        SourceUpdatedAt = entity.SourceUpdatedAt,
        DataVersion = entity.DataVersion
    };

    public static PlantDiaryDto ToDto(this PlantDiaryModel entity, IEnumerable<PlantImageModel>? images = null) => new()
    {
        Id = entity.ID,
        PlantId = entity.PlantID,
        DiaryDate = entity.DiaryDate,
        Title = entity.Title,
        Note = entity.Note,
        WeatherNote = entity.WeatherNote,
        EnvironmentNote = entity.EnvironmentNote,
        WateringNote = entity.WateringNote,
        FertilizerNote = entity.FertilizerNote,
        CreateDate = entity.CreateDate,
        Images = images?.Select(i => i.ToDto()).ToList() ?? []
    };

    public static PlantImageDto ToDto(this PlantImageModel entity) => new()
    {
        Id = entity.ID,
        PlantId = entity.PlantID,
        DiaryId = entity.DiaryID,
        Note = entity.Note,
        IsCover = entity.IsCover,
        FileName = entity.FileName,
        StoragePath = entity.StoragePath,
        ThumbnailPath = entity.ThumbnailPath,
        OriginalFileName = entity.OriginalFileName,
        ContentType = entity.ContentType,
        Width = entity.Width,
        Height = entity.Height,
        FileSize = entity.FileSize,
        CreateDate = entity.CreateDate
    };

    public static PlantAnalysisDto ToDto(this PlantAnalysisModel entity) => new()
    {
        Id = entity.ID,
        PlantId = entity.PlantID,
        DiaryId = entity.DiaryID,
        ImageId = entity.ImageID,
        AnalysisType = entity.AnalysisType,
        AnalysisScope = entity.AnalysisScope,
        ModelName = entity.ModelName,
        PromptVersion = entity.PromptVersion,
        Summary = entity.Summary,
        HealthScore = entity.HealthScore,
        Confidence = entity.Confidence,
        ResultJson = entity.ResultJson,
        CreateDate = entity.CreateDate
    };

    public static PlantEffectImageDto ToDto(this PlantEffectImageModel entity) => new()
    {
        Id = entity.ID,
        PlantId = entity.PlantID,
        OriginalPhotoId = entity.OriginalPhotoID,
        GeneratedImagePath = entity.GeneratedImagePath,
        Style = entity.Style,
        Layout = entity.Layout,
        ColorPalette = entity.ColorPalette,
        DecorationJson = entity.DecorationJson,
        PromptVersion = entity.PromptVersion,
        Status = entity.Status,
        GenerationRequestId = entity.GenerationRequestId,
        ErrorMessage = entity.ErrorMessage,
        IsLatest = entity.IsLatest,
        CreateDate = entity.CreateDate
    };

    public static PlantAnalysisJobDto ToDto(this PlantAnalysisJobModel entity) => new()
    {
        Id = entity.ID,
        PlantId = entity.PlantID,
        DiaryId = entity.DiaryID,
        ImageId = entity.ImageID,
        AnalysisId = entity.AnalysisID,
        Status = entity.Status,
        AnalysisScope = entity.AnalysisScope,
        ErrorMessage = entity.ErrorMessage,
        CreateDate = entity.CreateDate,
        CompletedAt = entity.CompletedAt
    };

    public static PlantSourceContentDto ToDto(this PlantSourceContentModel entity, PlantSourceModel? source = null) => new()
    {
        Id = entity.ID,
        SourceId = entity.SourceID,
        SourceTitle = source?.Title,
        SourceUrl = source?.Url,
        ReliabilityLevel = source?.ReliabilityLevel ?? 3,
        CleanText = entity.CleanText,
        Summary = entity.Summary,
        Keywords = entity.Keywords
    };

    public static PlantCareRecordDto ToDto(this PlantCareRecordModel entity) => new()
    {
        Id = entity.ID,
        PlantId = entity.PlantID,
        RecordDate = entity.RecordDate,
        CareType = entity.CareType,
        FertilizerProductId = entity.FertilizerProductID,
        NumericValue = entity.NumericValue,
        Unit = entity.Unit,
        Note = entity.Note,
        CreateDate = entity.CreateDate
    };

    public static PlantFertilizerProductDto ToDto(this PlantFertilizerProductModel entity) => new()
    {
        Id = entity.ID,
        PlantId = entity.PlantID,
        Name = entity.Name,
        IntervalDays = entity.IntervalDays,
        SortOrder = entity.SortOrder
    };

    public static PlantProfileDto ToDto(this PlantProfileModel entity) => new()
    {
        Id = entity.ID,
        PlantId = entity.PlantID,
        WateringIntervalDays = entity.WateringIntervalDays,
        FertilizingIntervalDays = entity.FertilizingIntervalDays,
        TargetHumidityMin = entity.TargetHumidityMin,
        TargetHumidityMax = entity.TargetHumidityMax,
        TargetTemperatureMin = entity.TargetTemperatureMin,
        TargetTemperatureMax = entity.TargetTemperatureMax,
        PersonalCareNotes = entity.PersonalCareNotes,
        ActualPlacement = entity.ActualPlacement,
        ActualLight = entity.ActualLight,
        HasRainCover = entity.HasRainCover,
        SubstrateType = entity.SubstrateType,
        SaucerState = entity.SaucerState,
        City = entity.City,
        OverrideSuggestedLight = entity.OverrideSuggestedLight,
        OverrideCareTaboosJson = entity.OverrideCareTaboosJson,
        WateringIntervalDetachedFromWiki = entity.WateringIntervalDetachedFromWiki,
        EnvironmentMismatchAcknowledged = entity.EnvironmentMismatchAcknowledged,
        AiEnvironmentAdvice = entity.AiEnvironmentAdvice
    };

    public static PlantReminderDto ToDto(this PlantReminderModel entity, string? plantName = null)
    {
        var today = DateTime.UtcNow.Date;
        return new PlantReminderDto
        {
            Id = entity.ID,
            PlantId = entity.PlantID,
            PlantName = plantName,
            ReminderType = entity.ReminderType,
            Priority = entity.Priority,
            Status = entity.Status,
            Title = entity.Title,
            Message = entity.Message,
            DueDate = entity.DueDate,
            IsOverdue = entity.DueDate.Date < today
        };
    }
}
