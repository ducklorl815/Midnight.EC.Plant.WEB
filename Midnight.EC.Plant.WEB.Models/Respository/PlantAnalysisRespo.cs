using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Enums;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantAnalysisRespo
{
    private readonly DBList _db;
    public PlantAnalysisRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID, ImageID,
       AnalysisType, AnalysisScope, ModelName, PromptVersion, InputSnapshot, ResultJson, Summary, HealthScore, Confidence
FROM dbo.PlantAnalysis WHERE Deleted = 0";

    public async Task<List<PlantAnalysisModel>> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantAnalysisModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND PlantID = @PlantId ORDER BY CreateDate DESC",
            new { PlantId = plantId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<PlantAnalysisModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantAnalysisModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(PlantAnalysisModel analysis, CancellationToken cancellationToken = default)
    {
        analysis.ID = analysis.ID == Guid.Empty ? Guid.NewGuid() : analysis.ID;
        var now = DateTime.UtcNow;
        analysis.CreateDate = analysis.CreateDate == default ? now : analysis.CreateDate;
        analysis.ModifyDate = now;
        analysis.Enabled = true;
        analysis.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantAnalysis
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID, ImageID,
     AnalysisType, AnalysisScope, ModelName, PromptVersion, InputSnapshot, ResultJson, Summary, HealthScore, Confidence)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID, @DiaryID, @ImageID,
     @AnalysisType, @AnalysisScope, @ModelName, @PromptVersion, @InputSnapshot, @ResultJson, @Summary, @HealthScore, @Confidence);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        analysis.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(analysis), cancellationToken: cancellationToken));
    }
}

public class PlantAnalysisJobRespo
{
    private readonly DBList _db;
    public PlantAnalysisJobRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID, ImageID, AnalysisID,
       Status, AnalysisScope, RetryCount, StartedAt, CompletedAt, ErrorMessage
FROM dbo.PlantAnalysisJob WHERE Deleted = 0";

    public async Task<PlantAnalysisJobModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantAnalysisJobModel>(new CommandDefinition(
            SelectSql + " AND ID = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<List<PlantAnalysisJobModel>> GetPendingJobsAsync(int take, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return (await conn.QueryAsync<PlantAnalysisJobModel>(new CommandDefinition(
            SelectSql + " AND Enabled = 1 AND Status = @Status ORDER BY CreateDate OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY",
            new { Status = (int)AnalysisJobStatus.Pending, Take = take }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task InsertAsync(PlantAnalysisJobModel job, CancellationToken cancellationToken = default)
    {
        job.ID = job.ID == Guid.Empty ? Guid.NewGuid() : job.ID;
        var now = DateTime.UtcNow;
        job.CreateDate = job.CreateDate == default ? now : job.CreateDate;
        job.ModifyDate = now;
        job.Enabled = true;
        job.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantAnalysisJob
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID, DiaryID, ImageID, AnalysisID,
     Status, AnalysisScope, RetryCount, StartedAt, CompletedAt, ErrorMessage)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID, @DiaryID, @ImageID, @AnalysisID,
     @Status, @AnalysisScope, @RetryCount, @StartedAt, @CompletedAt, @ErrorMessage);
SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = Conn();
        job.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(job), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantAnalysisJobModel job, CancellationToken cancellationToken = default)
    {
        job.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantAnalysisJob SET
    ModifyDate = @ModifyDate, AnalysisID = @AnalysisID, Status = @Status, AnalysisScope = @AnalysisScope,
    RetryCount = @RetryCount, StartedAt = @StartedAt, CompletedAt = @CompletedAt, ErrorMessage = @ErrorMessage
WHERE ID = @ID AND Deleted = 0";
        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(job), cancellationToken: cancellationToken));
    }
}
