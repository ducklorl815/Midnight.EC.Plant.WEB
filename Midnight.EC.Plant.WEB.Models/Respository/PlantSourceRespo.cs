using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantSourceRespo
{
    private readonly DBList _db;
    public PlantSourceRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID,
       SourceType, Title, Url, Domain, Author, PublishedAt, Language, ContentHash, ReliabilityLevel
FROM dbo.PlantSource WHERE Deleted = 0";

    public async Task<List<PlantSourceModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantSourceModel>(new CommandDefinition(
            SelectSql + " ORDER BY CreateDate DESC", cancellationToken: cancellationToken))).ToList();
    }

    public async Task<List<PlantSourceModel>> GetBySpeciesIdAsync(Guid speciesId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantSourceModel>(new CommandDefinition(@"
SELECT DISTINCT s.ID, s.Seq, s.CreateDate, s.ModifyDate, s.Enabled, s.Deleted, s.SpeciesID,
       s.SourceType, s.Title, s.Url, s.Domain, s.Author, s.PublishedAt, s.Language, s.ContentHash, s.ReliabilityLevel
FROM dbo.PlantSource s
LEFT JOIN dbo.PlantSourceSpecies ls ON ls.SourceID = s.ID AND ls.Deleted = 0
WHERE s.Deleted = 0 AND s.Enabled = 1
  AND (s.SpeciesID = @SpeciesId OR ls.SpeciesID = @SpeciesId)",
            new { SpeciesId = speciesId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<PlantSourceModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantSourceModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<PlantSourceModel?> GetByUrlHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantSourceModel>(new CommandDefinition(
            SelectSql + " AND ContentHash = @Hash", new { Hash = contentHash }, cancellationToken: cancellationToken));
    }

    public async Task<List<PlantSourceContentModel>> GetContentsBySourceIdAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantSourceContentModel>(new CommandDefinition(@"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, SourceID,
       RawText, CleanText, Summary, Keywords, ParsedJson, ParserType, ParserVersion, ContentHash, Status, ErrorMessage, ParsedAt
FROM dbo.PlantSourceContent WHERE Deleted = 0 AND SourceID = @SourceId",
            new { SourceId = sourceId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<List<Guid>> GetLinkedSpeciesIdsAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<Guid>(new CommandDefinition(
            @"SELECT SpeciesID FROM dbo.PlantSourceSpecies WHERE Deleted = 0 AND Enabled = 1 AND SourceID = @SourceId",
            new { SourceId = sourceId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task InsertAsync(PlantSourceModel source, CancellationToken cancellationToken = default)
    {
        source.ID = source.ID == Guid.Empty ? Guid.NewGuid() : source.ID;
        var now = DateTime.UtcNow;
        source.CreateDate = source.CreateDate == default ? now : source.CreateDate;
        source.ModifyDate = now;
        source.Enabled = true;
        source.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantSource
    (ID, CreateDate, ModifyDate, Enabled, Deleted, SpeciesID, SourceType, Title, Url, Domain, Author, PublishedAt, Language, ContentHash, ReliabilityLevel)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @SpeciesID, @SourceType, @Title, @Url, @Domain, @Author, @PublishedAt, @Language, @ContentHash, @ReliabilityLevel);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        source.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(source), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantSourceModel source, CancellationToken cancellationToken = default)
    {
        source.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantSource SET
    ModifyDate = @ModifyDate, Enabled = @Enabled, SpeciesID = @SpeciesID, SourceType = @SourceType,
    Title = @Title, Url = @Url, Domain = @Domain, Author = @Author, PublishedAt = @PublishedAt,
    Language = @Language, ContentHash = @ContentHash, ReliabilityLevel = @ReliabilityLevel
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(source), cancellationToken: cancellationToken));
    }

    public async Task InsertContentAsync(PlantSourceContentModel content, CancellationToken cancellationToken = default)
    {
        content.ID = content.ID == Guid.Empty ? Guid.NewGuid() : content.ID;
        var now = DateTime.UtcNow;
        content.CreateDate = content.CreateDate == default ? now : content.CreateDate;
        content.ModifyDate = now;
        content.Enabled = true;
        content.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantSourceContent
    (ID, CreateDate, ModifyDate, Enabled, Deleted, SourceID, RawText, CleanText, Summary, Keywords, ParsedJson,
     ParserType, ParserVersion, ContentHash, Status, ErrorMessage, ParsedAt)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @SourceID, @RawText, @CleanText, @Summary, @Keywords, @ParsedJson,
     @ParserType, @ParserVersion, @ContentHash, @Status, @ErrorMessage, @ParsedAt);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        content.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(content), cancellationToken: cancellationToken));
    }

    public async Task UpdateContentAsync(PlantSourceContentModel content, CancellationToken cancellationToken = default)
    {
        content.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantSourceContent SET
    ModifyDate = @ModifyDate, RawText = @RawText, CleanText = @CleanText, Summary = @Summary, Keywords = @Keywords,
    ParsedJson = @ParsedJson, ParserType = @ParserType, ParserVersion = @ParserVersion, ContentHash = @ContentHash,
    Status = @Status, ErrorMessage = @ErrorMessage, ParsedAt = @ParsedAt
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(content), cancellationToken: cancellationToken));
    }

    public async Task LinkSpeciesAsync(Guid sourceId, Guid speciesId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        var exists = await conn.ExecuteScalarAsync<int>(new CommandDefinition(@"
SELECT COUNT(1) FROM dbo.PlantSourceSpecies WHERE Deleted = 0 AND SourceID = @SourceId AND SpeciesID = @SpeciesId",
            new { SourceId = sourceId, SpeciesId = speciesId }, cancellationToken: cancellationToken));
        if (exists > 0) return;

        await conn.ExecuteAsync(new CommandDefinition(@"
INSERT INTO dbo.PlantSourceSpecies (ID, CreateDate, ModifyDate, Enabled, Deleted, SourceID, SpeciesID)
VALUES (@ID, @Now, @Now, 1, 0, @SourceId, @SpeciesId)",
            new { ID = Guid.NewGuid(), Now = DateTime.UtcNow, SourceId = sourceId, SpeciesId = speciesId },
            cancellationToken: cancellationToken));
    }
}
