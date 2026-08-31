using Midnight.EC.Plant.WEB.Models.AI;
using Midnight.EC.Plant.WEB.Models.DTOs;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Extensions;

public static class EntityMappingExtensions
{
    public static PlantDto ToDto(this Entities.Plant entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        SpeciesId = entity.SpeciesId,
        NickName = entity.NickName,
        Description = entity.Description,
        Location = entity.Location,
        EnvironmentNote = entity.EnvironmentNote,
        PurchaseDate = entity.PurchaseDate,
        StartDate = entity.StartDate,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Species = entity.Species?.ToDto(),
        Knowledge = entity.Species?.Knowledge?.ToDto()
    };

    public static PlantSpeciesDto ToDto(this PlantSpecies entity) => new()
    {
        Id = entity.Id,
        ScientificName = entity.ScientificName,
        CommonName = entity.CommonName,
        ChineseName = entity.ChineseName,
        Genus = entity.Genus,
        Family = entity.Family,
        TaxonId = entity.TaxonId,
        ImageUrl = entity.ImageUrl
    };

    public static PlantKnowledgeDto ToDto(this PlantKnowledge entity) => new()
    {
        Id = entity.Id,
        SpeciesId = entity.SpeciesId,
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
        SourceUpdatedAt = entity.SourceUpdatedAt,
        DataVersion = entity.DataVersion
    };

    public static PlantDiaryDto ToDto(this PlantDiary entity) => new()
    {
        Id = entity.Id,
        PlantId = entity.PlantId,
        DiaryDate = entity.DiaryDate,
        Title = entity.Title,
        Note = entity.Note,
        WeatherNote = entity.WeatherNote,
        EnvironmentNote = entity.EnvironmentNote,
        WateringNote = entity.WateringNote,
        FertilizerNote = entity.FertilizerNote,
        CreatedAt = entity.CreatedAt,
        Images = entity.Images.Select(i => i.ToDto()).ToList()
    };

    public static PlantImageDto ToDto(this PlantImage entity) => new()
    {
        Id = entity.Id,
        PlantId = entity.PlantId,
        DiaryId = entity.DiaryId,
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
        CreatedAt = entity.CreatedAt
    };

    public static PlantAnalysisDto ToDto(this PlantAnalysis entity) => new()
    {
        Id = entity.Id,
        PlantId = entity.PlantId,
        DiaryId = entity.DiaryId,
        ImageId = entity.ImageId,
        AnalysisType = entity.AnalysisType,
        AnalysisScope = entity.AnalysisScope,
        ModelName = entity.ModelName,
        PromptVersion = entity.PromptVersion,
        Summary = entity.Summary,
        HealthScore = entity.HealthScore,
        Confidence = entity.Confidence,
        ResultJson = entity.ResultJson,
        CreatedAt = entity.CreatedAt
    };

    public static PlantAnalysisJobDto ToDto(this PlantAnalysisJob entity) => new()
    {
        Id = entity.Id,
        PlantId = entity.PlantId,
        DiaryId = entity.DiaryId,
        ImageId = entity.ImageId,
        AnalysisId = entity.AnalysisId,
        Status = entity.Status,
        AnalysisScope = entity.AnalysisScope,
        ErrorMessage = entity.ErrorMessage,
        CreatedAt = entity.CreatedAt,
        CompletedAt = entity.CompletedAt
    };

    public static PlantSourceContentDto ToDto(this PlantSourceContent entity, PlantSource? source = null) => new()
    {
        Id = entity.Id,
        SourceId = entity.SourceId,
        SourceTitle = source?.Title,
        SourceUrl = source?.Url,
        ReliabilityLevel = source?.ReliabilityLevel ?? 3,
        CleanText = entity.CleanText,
        Summary = entity.Summary,
        Keywords = entity.Keywords
    };

    public static PlantCareRecordDto ToDto(this PlantCareRecord entity) => new()
    {
        Id = entity.Id,
        PlantId = entity.PlantId,
        RecordDate = entity.RecordDate,
        CareType = entity.CareType,
        NumericValue = entity.NumericValue,
        Unit = entity.Unit,
        Note = entity.Note,
        CreatedAt = entity.CreatedAt
    };

    public static PlantProfileDto ToDto(this PlantProfile entity) => new()
    {
        Id = entity.Id,
        PlantId = entity.PlantId,
        WateringIntervalDays = entity.WateringIntervalDays,
        FertilizingIntervalDays = entity.FertilizingIntervalDays,
        TargetHumidityMin = entity.TargetHumidityMin,
        TargetHumidityMax = entity.TargetHumidityMax,
        TargetTemperatureMin = entity.TargetTemperatureMin,
        TargetTemperatureMax = entity.TargetTemperatureMax,
        PersonalCareNotes = entity.PersonalCareNotes
    };

    public static PlantReminderDto ToDto(this PlantReminder entity, string? plantName = null)
    {
        var today = DateTime.UtcNow.Date;
        return new PlantReminderDto
        {
            Id = entity.Id,
            PlantId = entity.PlantId,
            PlantName = plantName ?? entity.Plant?.Name,
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
