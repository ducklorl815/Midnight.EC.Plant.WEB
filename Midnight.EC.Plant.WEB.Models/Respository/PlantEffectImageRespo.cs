using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantEffectImageRespo
{
    private readonly DBList _db;
    public PlantEffectImageRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID, OriginalPhotoID,
       GeneratedImagePath, Style, Layout, ColorPalette, DecorationJson, PromptVersion, PromptText,
       Status, GenerationRequestId, ErrorMessage, IsLatest
FROM dbo.PlantEffectImage WHERE Deleted = 0";

    public async Task<PlantEffectImageModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantEffectImageModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<PlantEffectImageModel?> GetLatestByOriginalPhotoIdAsync(
        Guid originalPhotoId,
        CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantEffectImageModel>(new CommandDefinition(
            SelectSql + @" AND Enabled = 1 AND OriginalPhotoID = @OriginalPhotoId AND IsLatest = 1
                           AND Status = @Status
                           ORDER BY CreateDate DESC",
            new { OriginalPhotoId = originalPhotoId, Status = (int)EffectImageStatus.Completed },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyDictionary<Guid, PlantEffectImageModel>> GetLatestByOriginalPhotoIdsAsync(
        IReadOnlyCollection<Guid> originalPhotoIds,
        CancellationToken cancellationToken = default)
    {
        if (originalPhotoIds.Count == 0)
            return new Dictionary<Guid, PlantEffectImageModel>();

        using var conn = Conn();
        var rows = (await conn.QueryAsync<PlantEffectImageModel>(new CommandDefinition(
            SelectSql + @" AND Enabled = 1 AND IsLatest = 1 AND Status = @Status
                           AND OriginalPhotoID IN @Ids
                           ORDER BY CreateDate DESC",
            new { Ids = originalPhotoIds.ToArray(), Status = (int)EffectImageStatus.Completed },
            cancellationToken: cancellationToken))).ToList();

        return rows
            .GroupBy(r => r.OriginalPhotoID)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task ClearLatestAsync(Guid originalPhotoId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE dbo.PlantEffectImage SET IsLatest = 0, ModifyDate = @Now
              WHERE OriginalPhotoID = @OriginalPhotoId AND Deleted = 0 AND IsLatest = 1",
            new { OriginalPhotoId = originalPhotoId, Now = DateTime.UtcNow },
            cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantEffectImageModel row, CancellationToken cancellationToken = default)
    {
        row.ID = row.ID == Guid.Empty ? Guid.NewGuid() : row.ID;
        var now = DateTime.UtcNow;
        row.CreateDate = row.CreateDate == default ? now : row.CreateDate;
        row.ModifyDate = now;
        row.Enabled = true;
        row.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantEffectImage
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID, OriginalPhotoID,
     GeneratedImagePath, Style, Layout, ColorPalette, DecorationJson, PromptVersion, PromptText,
     Status, GenerationRequestId, ErrorMessage, IsLatest)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID, @OriginalPhotoID,
     @GeneratedImagePath, @Style, @Layout, @ColorPalette, @DecorationJson, @PromptVersion, @PromptText,
     @Status, @GenerationRequestId, @ErrorMessage, @IsLatest);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var conn = Conn();
        row.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, DbParams.From(row), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantEffectImageModel row, CancellationToken cancellationToken = default)
    {
        row.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantEffectImage SET
    ModifyDate = @ModifyDate, Enabled = @Enabled,
    GeneratedImagePath = @GeneratedImagePath, Style = @Style, Layout = @Layout,
    ColorPalette = @ColorPalette, DecorationJson = @DecorationJson,
    PromptVersion = @PromptVersion, PromptText = @PromptText,
    Status = @Status, GenerationRequestId = @GenerationRequestId,
    ErrorMessage = @ErrorMessage, IsLatest = @IsLatest
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(row), cancellationToken: cancellationToken));
    }
}
