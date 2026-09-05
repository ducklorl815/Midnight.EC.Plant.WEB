using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantDiaryRespo
{
    private readonly DBList _db;
    public PlantDiaryRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
       DiaryDate, Title, Note, WeatherNote, EnvironmentNote, WateringNote, FertilizerNote
FROM dbo.PlantDiary WHERE Deleted = 0";

    public async Task<List<PlantDiaryModel>> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantDiaryModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId ORDER BY DiaryDate DESC",
            new { PlantId = plantId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<List<PlantDiaryModel>> GetRecentByPlantIdAsync(Guid plantId, DateTime since, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantDiaryModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId AND DiaryDate >= @Since ORDER BY DiaryDate DESC",
            new { PlantId = plantId, Since = since }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<PlantDiaryModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantDiaryModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantDiaryModel diary, CancellationToken cancellationToken = default)
    {
        diary.ID = diary.ID == Guid.Empty ? Guid.NewGuid() : diary.ID;
        var now = DateTime.UtcNow;
        diary.CreateDate = diary.CreateDate == default ? now : diary.CreateDate;
        diary.ModifyDate = now;
        diary.Enabled = true;
        diary.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantDiary
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryDate, Title, Note, WeatherNote, EnvironmentNote, WateringNote, FertilizerNote)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID, @DiaryDate, @Title, @Note, @WeatherNote, @EnvironmentNote, @WateringNote, @FertilizerNote);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        diary.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(diary), cancellationToken: cancellationToken));
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE dbo.PlantDiary SET Deleted = 1, Enabled = 0, ModifyDate = @Now WHERE ID = @Id",
            new { Id = id, Now = DateTime.UtcNow }, cancellationToken: cancellationToken));
    }
}
