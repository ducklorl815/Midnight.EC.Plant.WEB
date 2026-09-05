using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantCareRecordRespo
{
    private readonly DBList _db;
    public PlantCareRecordRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
       RecordDate, CareType, NumericValue, Unit, Note
FROM dbo.PlantCareRecord WHERE Deleted = 0";

    public async Task<List<PlantCareRecordModel>> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantCareRecordModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId ORDER BY RecordDate DESC",
            new { PlantId = plantId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<List<PlantCareRecordModel>> GetRecentByPlantIdAsync(Guid plantId, DateTime since, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantCareRecordModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId AND RecordDate >= @Since ORDER BY RecordDate DESC",
            new { PlantId = plantId, Since = since }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<Dictionary<Guid, DateTime>> GetLastWateringDatesAsync(IEnumerable<Guid> plantIds, CancellationToken cancellationToken = default)
    {
        var idList = plantIds.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, DateTime>();

        using var conn = Conn();
        var rows = await conn.QueryAsync<(Guid PlantID, DateTime LastDate)>(new CommandDefinition(@"
SELECT PlantID, MAX(RecordDate) AS LastDate
FROM dbo.PlantCareRecord
WHERE Deleted = 0 AND Enabled = 1 AND CareType = @CareType AND PlantID IN @Ids
GROUP BY PlantID",
            new { CareType = (int)CareRecordType.Watering, Ids = idList }, cancellationToken: cancellationToken));
        return rows.ToDictionary(r => r.PlantID, r => r.LastDate);
    }

    public async Task<PlantCareRecordModel?> FindSameDayAsync(
        Guid plantId, DateTime recordDate, CareRecordType careType, CancellationToken cancellationToken = default)
    {
        var day = recordDate.Date;
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantCareRecordModel>(new CommandDefinition(
            SelectSql + @" AND Enabled = 1 AND PlantID = @PlantId AND CareType = @CareType
                           AND CONVERT(date, RecordDate) = @Day",
            new { PlantId = plantId, CareType = (int)careType, Day = day }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantCareRecordModel record, CancellationToken cancellationToken = default)
    {
        record.ID = record.ID == Guid.Empty ? Guid.NewGuid() : record.ID;
        var now = DateTime.UtcNow;
        record.CreateDate = record.CreateDate == default ? now : record.CreateDate;
        record.ModifyDate = now;
        record.Enabled = true;
        record.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantCareRecord
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID, RecordDate, CareType, NumericValue, Unit, Note)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID, @RecordDate, @CareType, @NumericValue, @Unit, @Note);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        record.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(record), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantCareRecordModel record, CancellationToken cancellationToken = default)
    {
        record.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantCareRecord SET
    ModifyDate = @ModifyDate, RecordDate = @RecordDate, CareType = @CareType,
    NumericValue = @NumericValue, Unit = @Unit, Note = @Note
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(record), cancellationToken: cancellationToken));
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE dbo.PlantCareRecord SET Deleted = 1, Enabled = 0, ModifyDate = @Now WHERE ID = @Id",
            new { Id = id, Now = DateTime.UtcNow }, cancellationToken: cancellationToken));
    }
}
