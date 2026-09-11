using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PageComposerLayoutRespo
{
    private readonly DBList _db;

    public PageComposerLayoutRespo(IOptions<DBList> dbList) => _db = dbList.Value;

    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, LayoutKey, LayoutJson
FROM dbo.PageComposerLayout
WHERE Deleted = 0";

    public async Task<PageComposerLayoutModel?> GetByKeyAsync(string layoutKey, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PageComposerLayoutModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND LayoutKey = @LayoutKey",
            new { LayoutKey = layoutKey },
            cancellationToken: cancellationToken));
    }

    public async Task UpsertAsync(PageComposerLayoutModel model, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        model.ModifyDate = now;
        model.Enabled = true;
        model.Deleted = false;

        using var conn = Conn();
        var existing = await conn.QueryFirstOrDefaultAsync<PageComposerLayoutModel>(new CommandDefinition(
            SelectSql + " AND LayoutKey = @LayoutKey",
            new { model.LayoutKey },
            cancellationToken: cancellationToken));

        if (existing == null)
        {
            model.ID = model.ID == Guid.Empty ? Guid.NewGuid() : model.ID;
            model.CreateDate = model.CreateDate == default ? now : model.CreateDate;
            const string insertSql = @"
INSERT INTO dbo.PageComposerLayout
    (ID, CreateDate, ModifyDate, Enabled, Deleted, LayoutKey, LayoutJson)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @LayoutKey, @LayoutJson);
SELECT CAST(SCOPE_IDENTITY() AS int);";
            model.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                insertSql, DbParams.From(model), cancellationToken: cancellationToken));
            return;
        }

        model.ID = existing.ID;
        model.CreateDate = existing.CreateDate;
        const string updateSql = @"
UPDATE dbo.PageComposerLayout SET
    ModifyDate = @ModifyDate,
    Enabled = @Enabled,
    LayoutJson = @LayoutJson
WHERE ID = @ID AND Deleted = 0";
        await conn.ExecuteAsync(new CommandDefinition(
            updateSql, DbParams.From(model), cancellationToken: cancellationToken));
    }
}
