using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantImageRespo
{
    private readonly DBList _db;
    public PlantImageRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID,
       Note, IsCover, FileName, StoragePath, ThumbnailPath, OriginalFileName,
       ContentType, Width, Height, FileSize, Sha256
FROM dbo.PlantImage WHERE Deleted = 0";

    public async Task<PlantImageModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantImageModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<List<PlantImageModel>> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantImageModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId ORDER BY IsCover DESC, CreateDate DESC",
            new { PlantId = plantId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<PlantImageModel?> GetCoverByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantImageModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId AND IsCover = 1",
            new { PlantId = plantId }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantImageModel image, CancellationToken cancellationToken = default)
    {
        image.ID = image.ID == Guid.Empty ? Guid.NewGuid() : image.ID;
        var now = DateTime.UtcNow;
        image.CreateDate = image.CreateDate == default ? now : image.CreateDate;
        image.ModifyDate = now;
        image.Enabled = true;
        image.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantImage
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID, Note, IsCover,
     FileName, StoragePath, ThumbnailPath, OriginalFileName, ContentType, Width, Height, FileSize, Sha256)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID, @DiaryID, @Note, @IsCover,
     @FileName, @StoragePath, @ThumbnailPath, @OriginalFileName, @ContentType, @Width, @Height, @FileSize, @Sha256);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        image.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(image), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantImageModel image, CancellationToken cancellationToken = default)
    {
        image.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantImage SET
    ModifyDate = @ModifyDate, Enabled = @Enabled, Note = @Note, IsCover = @IsCover,
    DiaryID = @DiaryID, FileName = @FileName, StoragePath = @StoragePath, ThumbnailPath = @ThumbnailPath
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(image), cancellationToken: cancellationToken));
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE dbo.PlantImage SET Deleted = 1, Enabled = 0, ModifyDate = @Now WHERE ID = @Id",
            new { Id = id, Now = DateTime.UtcNow }, cancellationToken: cancellationToken));
    }

    public async Task ClearCoverAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE dbo.PlantImage SET IsCover = 0, ModifyDate = @Now WHERE PlantID = @PlantId AND Deleted = 0 AND IsCover = 1",
            new { PlantId = plantId, Now = DateTime.UtcNow }, cancellationToken: cancellationToken));
    }
}
