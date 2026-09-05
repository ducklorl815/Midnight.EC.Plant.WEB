using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantRespo
{
    private readonly DBList _db;

    public PlantRespo(IOptions<DBList> dbList) => _db = dbList.Value;

    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string PlantSelect = @"
SELECT p.ID, p.Seq, p.CreateDate, p.ModifyDate, p.Enabled, p.Deleted,
       p.Name, p.SpeciesID, p.NickName, p.Description, p.Location, p.EnvironmentNote,
       p.PurchaseDate, p.StartDate
FROM dbo.Plant p
WHERE p.Deleted = 0 AND p.Enabled = 1";

    public async Task<List<PlantModel>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        var plants = (await conn.QueryAsync<PlantModel>(
            new CommandDefinition(PlantSelect + " ORDER BY p.ModifyDate DESC", cancellationToken: cancellationToken))).ToList();
        await AttachSpeciesKnowledgeAsync(conn, plants, cancellationToken);
        return plants;
    }

    public async Task<List<PlantModel>> GetByIdsWithDetailsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return [];

        using var conn = Conn();
        var plants = (await conn.QueryAsync<PlantModel>(
            new CommandDefinition(
                PlantSelect + " AND p.ID IN @Ids ORDER BY p.Name",
                new { Ids = idList },
                cancellationToken: cancellationToken))).ToList();
        await AttachSpeciesKnowledgeAsync(conn, plants, cancellationToken);
        await AttachProfilesAsync(conn, plants, cancellationToken);
        return plants;
    }

    public async Task<PlantModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantModel>(
            new CommandDefinition(
                PlantSelect + " AND p.ID = @Id",
                new { Id = id },
                cancellationToken: cancellationToken));
    }

    public async Task<PlantModel?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        var plant = await conn.QueryFirstOrDefaultAsync<PlantModel>(
            new CommandDefinition(
                PlantSelect + " AND p.ID = @Id",
                new { Id = id },
                cancellationToken: cancellationToken));
        if (plant == null) return null;
        await AttachSpeciesKnowledgeAsync(conn, [plant], cancellationToken);
        return plant;
    }

    public async Task<bool> IsNickNameTakenAsync(string nickName, Guid? excludePlantId = null, CancellationToken cancellationToken = default)
    {
        var key = nickName.Trim();
        using var conn = Conn();
        var sql = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.Plant
    WHERE Deleted = 0 AND Enabled = 1 AND NickName = @Key
      AND (@Exclude IS NULL OR ID <> @Exclude)
) THEN 1 ELSE 0 END";
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Key = key, Exclude = excludePlantId }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantModel plant, CancellationToken cancellationToken = default)
    {
        plant.ID = plant.ID == Guid.Empty ? Guid.NewGuid() : plant.ID;
        var now = DateTime.UtcNow;
        plant.CreateDate = plant.CreateDate == default ? now : plant.CreateDate;
        plant.ModifyDate = now;
        plant.Enabled = true;
        plant.Deleted = false;

        const string sql = @"
INSERT INTO dbo.Plant
    (ID, CreateDate, ModifyDate, Enabled, Deleted, Name, SpeciesID, NickName, Description, Location, EnvironmentNote, PurchaseDate, StartDate)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @Name, @SpeciesID, @NickName, @Description, @Location, @EnvironmentNote, @PurchaseDate, @StartDate);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var conn = Conn();
        plant.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(plant), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantModel plant, CancellationToken cancellationToken = default)
    {
        plant.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.Plant SET
    ModifyDate = @ModifyDate,
    Enabled = @Enabled,
    Name = @Name,
    SpeciesID = @SpeciesID,
    NickName = @NickName,
    Description = @Description,
    Location = @Location,
    EnvironmentNote = @EnvironmentNote,
    PurchaseDate = @PurchaseDate,
    StartDate = @StartDate
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(plant), cancellationToken: cancellationToken));
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE dbo.Plant SET Deleted = 1, Enabled = 0, ModifyDate = @Now WHERE ID = @Id",
            new { Id = id, Now = DateTime.UtcNow },
            cancellationToken: cancellationToken));
    }

    private static async Task AttachSpeciesKnowledgeAsync(SqlConnection conn, List<PlantModel> plants, CancellationToken ct)
    {
        var speciesIds = plants.Select(p => p.SpeciesID).Distinct().ToList();
        if (speciesIds.Count == 0) return;

        var species = (await conn.QueryAsync<PlantSpeciesModel>(new CommandDefinition(@"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted,
       ScientificName, CommonName, ChineseName, Genus, Family, TaxonId, ImageUrl, SourceType, SourceId
FROM dbo.PlantSpecies
WHERE Deleted = 0 AND ID IN @Ids", new { Ids = speciesIds }, cancellationToken: ct))).ToDictionary(s => s.ID);

        var knowledge = (await conn.QueryAsync<PlantKnowledgeModel>(new CommandDefinition(@"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID,
       LightRequirement, WaterRequirement, HumidityRequirement, TemperatureMin, TemperatureMax,
       SoilRequirement, FertilizerRequirement, Dormancy, GrowthSeason, RepottingAdvice,
       CommonProblems, PestProblems, DiseaseProblems, CareSummary, ExternalCareGuide,
       SuggestedLight, CareTaboosJson, SuggestedWateringIntervalDays, SourceUpdatedAt, DataVersion
FROM dbo.PlantKnowledge
WHERE Deleted = 0 AND SpeciesID IN @Ids", new { Ids = speciesIds }, cancellationToken: ct)))
            .ToDictionary(k => k.SpeciesID);

        foreach (var s in species.Values)
        {
            if (knowledge.TryGetValue(s.ID, out var k)) s.Knowledge = k;
        }

        foreach (var p in plants)
        {
            if (species.TryGetValue(p.SpeciesID, out var s)) p.Species = s;
        }
    }

    private static async Task AttachProfilesAsync(SqlConnection conn, List<PlantModel> plants, CancellationToken ct)
    {
        var plantIds = plants.Select(p => p.ID).ToList();
        if (plantIds.Count == 0) return;

        var profiles = (await conn.QueryAsync<PlantProfileModel>(new CommandDefinition(@"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
       WateringIntervalDays, FertilizingIntervalDays, TargetHumidityMin, TargetHumidityMax,
       TargetTemperatureMin, TargetTemperatureMax, PersonalCareNotes,
       ActualPlacement, ActualLight, HasRainCover, SubstrateType, SaucerState, City,
       OverrideSuggestedLight, OverrideCareTaboosJson,
       WateringIntervalDetachedFromWiki, EnvironmentMismatchAcknowledged, AiEnvironmentAdvice
FROM dbo.PlantProfile
WHERE Deleted = 0 AND PlantID IN @Ids", new { Ids = plantIds }, cancellationToken: ct)))
            .ToDictionary(x => x.PlantID);

        foreach (var p in plants)
        {
            if (profiles.TryGetValue(p.ID, out var pr)) p.Profile = pr;
        }
    }
}
