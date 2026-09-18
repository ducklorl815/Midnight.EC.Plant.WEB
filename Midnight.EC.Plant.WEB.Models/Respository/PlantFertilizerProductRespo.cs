using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantFertilizerProductRespo
{
    private readonly DBList _db;
    public PlantFertilizerProductRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID, Name, IntervalDays, SortOrder
FROM dbo.PlantFertilizerProduct WHERE Deleted = 0";

    public async Task<List<PlantFertilizerProductModel>> GetByPlantIdAsync(
        Guid plantId,
        CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantFertilizerProductModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId ORDER BY SortOrder, CreateDate",
            new { PlantId = plantId },
            cancellationToken: cancellationToken))).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, List<PlantFertilizerProductModel>>> GetByPlantIdsAsync(
        IReadOnlyCollection<Guid> plantIds,
        CancellationToken cancellationToken = default)
    {
        if (plantIds.Count == 0)
            return new Dictionary<Guid, List<PlantFertilizerProductModel>>();

        using var conn = Conn();
        var rows = (await conn.QueryAsync<PlantFertilizerProductModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID IN @PlantIds ORDER BY SortOrder, CreateDate",
            new { PlantIds = plantIds.ToArray() },
            cancellationToken: cancellationToken))).ToList();

        return rows
            .GroupBy(r => r.PlantID)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<PlantFertilizerProductModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantFertilizerProductModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantFertilizerProductModel product, CancellationToken cancellationToken = default)
    {
        product.ID = product.ID == Guid.Empty ? Guid.NewGuid() : product.ID;
        var now = DateTime.UtcNow;
        product.CreateDate = product.CreateDate == default ? now : product.CreateDate;
        product.ModifyDate = now;
        product.Enabled = true;
        product.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantFertilizerProduct
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID, Name, IntervalDays, SortOrder)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID, @Name, @IntervalDays, @SortOrder);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var conn = Conn();
        product.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, DbParams.From(product), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantFertilizerProductModel product, CancellationToken cancellationToken = default)
    {
        product.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantFertilizerProduct SET
    ModifyDate = @ModifyDate, Name = @Name, IntervalDays = @IntervalDays, SortOrder = @SortOrder
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(product), cancellationToken: cancellationToken));
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE dbo.PlantFertilizerProduct SET Deleted = 1, Enabled = 0, ModifyDate = @Now WHERE ID = @Id",
            new { Id = id, Now = DateTime.UtcNow },
            cancellationToken: cancellationToken));
    }
}
