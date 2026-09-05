using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantReminderRespo
{
    private readonly DBList _db;
    public PlantReminderRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
       ReminderType, Priority, Status, Title, Message, DueDate, SourceKey, DismissedAt
FROM dbo.PlantReminder WHERE Deleted = 0";

    public async Task<List<PlantReminderModel>> GetActiveByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantReminderModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId AND Status = @Status ORDER BY DueDate",
            new { PlantId = plantId, Status = (int)ReminderStatus.Active }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<List<PlantReminderModel>> GetActiveForPlantsAsync(IEnumerable<Guid> plantIds, CancellationToken cancellationToken = default)
    {
        var ids = plantIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        using var conn = Conn();
        return (await conn.QueryAsync<PlantReminderModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID IN @Ids AND Status = @Status ORDER BY DueDate",
            new { Ids = ids, Status = (int)ReminderStatus.Active }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<PlantReminderModel?> GetBySourceKeyAsync(Guid plantId, string sourceKey, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantReminderModel>(new CommandDefinition(
            SelectSql + " AND PlantID = @PlantId AND SourceKey = @SourceKey",
            new { PlantId = plantId, SourceKey = sourceKey }, cancellationToken: cancellationToken));
    }

    public async Task<PlantReminderModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantReminderModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantReminderModel reminder, CancellationToken cancellationToken = default)
    {
        reminder.ID = reminder.ID == Guid.Empty ? Guid.NewGuid() : reminder.ID;
        var now = DateTime.UtcNow;
        reminder.CreateDate = reminder.CreateDate == default ? now : reminder.CreateDate;
        reminder.ModifyDate = now;
        reminder.Enabled = true;
        reminder.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantReminder
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID, ReminderType, Priority, Status, Title, Message, DueDate, SourceKey, DismissedAt)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID, @ReminderType, @Priority, @Status, @Title, @Message, @DueDate, @SourceKey, @DismissedAt);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        reminder.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(reminder), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantReminderModel reminder, CancellationToken cancellationToken = default)
    {
        reminder.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantReminder SET
    ModifyDate = @ModifyDate, Enabled = @Enabled, ReminderType = @ReminderType, Priority = @Priority,
    Status = @Status, Title = @Title, Message = @Message, DueDate = @DueDate, SourceKey = @SourceKey, DismissedAt = @DismissedAt
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(reminder), cancellationToken: cancellationToken));
    }
}
