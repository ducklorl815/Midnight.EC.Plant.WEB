using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantKnowledgeRespo
{
    private readonly DBList _db;
    public PlantKnowledgeRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    public async Task<PlantKnowledgeModel?> GetBySpeciesIdAsync(Guid speciesId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantKnowledgeModel>(new CommandDefinition(@"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID,
       LightRequirement, WaterRequirement, HumidityRequirement, TemperatureMin, TemperatureMax,
       SoilRequirement, FertilizerRequirement, Dormancy, GrowthSeason, RepottingAdvice,
       CommonProblems, PestProblems, DiseaseProblems, CareSummary, ExternalCareGuide,
       SuggestedLight, CareTaboosJson, SuggestedWateringIntervalDays, SourceUpdatedAt, DataVersion
FROM dbo.PlantKnowledge WHERE Deleted = 0 AND SpeciesID = @SpeciesId",
            new { SpeciesId = speciesId }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantKnowledgeModel knowledge, CancellationToken cancellationToken = default)
    {
        knowledge.ID = knowledge.ID == Guid.Empty ? Guid.NewGuid() : knowledge.ID;
        var now = DateTime.UtcNow;
        knowledge.CreateDate = knowledge.CreateDate == default ? now : knowledge.CreateDate;
        knowledge.ModifyDate = now;
        knowledge.Enabled = true;
        knowledge.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantKnowledge
    (ID, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID,
     LightRequirement, WaterRequirement, HumidityRequirement, TemperatureMin, TemperatureMax,
     SoilRequirement, FertilizerRequirement, Dormancy, GrowthSeason, RepottingAdvice,
     CommonProblems, PestProblems, DiseaseProblems, CareSummary, ExternalCareGuide,
     SuggestedLight, CareTaboosJson, SuggestedWateringIntervalDays, SourceUpdatedAt, DataVersion)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @SpeciesID,
     @LightRequirement, @WaterRequirement, @HumidityRequirement, @TemperatureMin, @TemperatureMax,
     @SoilRequirement, @FertilizerRequirement, @Dormancy, @GrowthSeason, @RepottingAdvice,
     @CommonProblems, @PestProblems, @DiseaseProblems, @CareSummary, @ExternalCareGuide,
     @SuggestedLight, @CareTaboosJson, @SuggestedWateringIntervalDays, @SourceUpdatedAt, @DataVersion);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        knowledge.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(knowledge), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantKnowledgeModel knowledge, CancellationToken cancellationToken = default)
    {
        knowledge.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantKnowledge SET
    ModifyDate = @ModifyDate, Enabled = @Enabled,
    LightRequirement = @LightRequirement, WaterRequirement = @WaterRequirement, HumidityRequirement = @HumidityRequirement,
    TemperatureMin = @TemperatureMin, TemperatureMax = @TemperatureMax,
    SoilRequirement = @SoilRequirement, FertilizerRequirement = @FertilizerRequirement,
    Dormancy = @Dormancy, GrowthSeason = @GrowthSeason, RepottingAdvice = @RepottingAdvice,
    CommonProblems = @CommonProblems, PestProblems = @PestProblems, DiseaseProblems = @DiseaseProblems,
    CareSummary = @CareSummary, ExternalCareGuide = @ExternalCareGuide,
    SuggestedLight = @SuggestedLight, CareTaboosJson = @CareTaboosJson,
    SuggestedWateringIntervalDays = @SuggestedWateringIntervalDays, SourceUpdatedAt = @SourceUpdatedAt, DataVersion = @DataVersion
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(knowledge), cancellationToken: cancellationToken));
    }
}
