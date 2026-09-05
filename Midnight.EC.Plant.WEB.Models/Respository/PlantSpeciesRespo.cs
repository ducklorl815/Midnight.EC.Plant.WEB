using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantSpeciesRespo
{
    private readonly DBList _db;
    public PlantSpeciesRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted,
       ScientificName, CommonName, ChineseName, Genus, Family, TaxonId, ImageUrl, SourceType, SourceId
FROM dbo.PlantSpecies
WHERE Deleted = 0";

    public async Task<List<PlantSpeciesModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantSpeciesModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 ORDER BY COALESCE(ChineseName, CommonName, ScientificName)",
            cancellationToken: cancellationToken))).ToList();
    }

    public async Task<PlantSpeciesModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        var species = await conn.QueryFirstOrDefaultAsync<PlantSpeciesModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id", new { Id = id }, cancellationToken: cancellationToken));
        if (species != null) species.Knowledge = await LoadKnowledgeAsync(conn, species.ID, cancellationToken);
        return species;
    }

    public async Task<PlantSpeciesModel?> GetByTaxonIdAsync(string taxonId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        var species = await conn.QueryFirstOrDefaultAsync<PlantSpeciesModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND TaxonId = @TaxonId", new { TaxonId = taxonId }, cancellationToken: cancellationToken));
        if (species != null) species.Knowledge = await LoadKnowledgeAsync(conn, species.ID, cancellationToken);
        return species;
    }

    public async Task<PlantSpeciesModel?> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var keyword = name.Trim();
        using var conn = Conn();
        var species = await conn.QueryFirstOrDefaultAsync<PlantSpeciesModel>(new CommandDefinition(
            SelectSql + @" AND Enabled = 1 AND (
                ScientificName LIKE '%' + @Keyword + '%'
                OR CommonName LIKE '%' + @Keyword + '%'
                OR ChineseName LIKE '%' + @Keyword + '%'
            ) ORDER BY ScientificName",
            new { Keyword = keyword }, cancellationToken: cancellationToken));
        if (species != null) species.Knowledge = await LoadKnowledgeAsync(conn, species.ID, cancellationToken);
        return species;
    }

    public async Task<PlantSpeciesModel?> GetByScientificNameExactAsync(string scientificName, CancellationToken cancellationToken = default)
    {
        var keyword = scientificName.Trim();
        using var conn = Conn();
        var species = await conn.QueryFirstOrDefaultAsync<PlantSpeciesModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND LOWER(ScientificName) = LOWER(@Keyword)",
            new { Keyword = keyword }, cancellationToken: cancellationToken));
        if (species != null) species.Knowledge = await LoadKnowledgeAsync(conn, species.ID, cancellationToken);
        return species;
    }

    public async Task InsertAsync(PlantSpeciesModel species, CancellationToken cancellationToken = default)
    {
        species.ID = species.ID == Guid.Empty ? Guid.NewGuid() : species.ID;
        var now = DateTime.UtcNow;
        species.CreateDate = species.CreateDate == default ? now : species.CreateDate;
        species.ModifyDate = now;
        species.Enabled = true;
        species.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantSpecies
    (ID, CreateDate, ModifyDate, Enabled, Deleted, ScientificName, CommonName, ChineseName, Genus, Family, TaxonId, ImageUrl, SourceType, SourceId)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @ScientificName, @CommonName, @ChineseName, @Genus, @Family, @TaxonId, @ImageUrl, @SourceType, @SourceId);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        species.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(species), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantSpeciesModel species, CancellationToken cancellationToken = default)
    {
        species.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantSpecies SET
    ModifyDate = @ModifyDate, Enabled = @Enabled,
    ScientificName = @ScientificName, CommonName = @CommonName, ChineseName = @ChineseName,
    Genus = @Genus, Family = @Family, TaxonId = @TaxonId, ImageUrl = @ImageUrl,
    SourceType = @SourceType, SourceId = @SourceId
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(species), cancellationToken: cancellationToken));
    }

    private static async Task<PlantKnowledgeModel?> LoadKnowledgeAsync(SqlConnection conn, Guid speciesId, CancellationToken ct) =>
        await conn.QueryFirstOrDefaultAsync<PlantKnowledgeModel>(new CommandDefinition(@"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID,
       LightRequirement, WaterRequirement, HumidityRequirement, TemperatureMin, TemperatureMax,
       SoilRequirement, FertilizerRequirement, Dormancy, GrowthSeason, RepottingAdvice,
       CommonProblems, PestProblems, DiseaseProblems, CareSummary, ExternalCareGuide,
       SuggestedLight, CareTaboosJson, SuggestedWateringIntervalDays, SourceUpdatedAt, DataVersion
FROM dbo.PlantKnowledge WHERE Deleted = 0 AND SpeciesID = @SpeciesId",
            new { SpeciesId = speciesId }, cancellationToken: ct));
}
