using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Midnight.EC.Plant.WEB.Models.Models;

namespace Midnight.EC.Plant.WEB.Models.Respository;

public class PlantProfileRespo
{
    private readonly DBList _db;
    public PlantProfileRespo(IOptions<DBList> dbList) => _db = dbList.Value;
    private SqlConnection Conn() => new(_db.DefaultConnection);

    private const string SelectSql = @"
SELECT ID, Seq, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
       WateringIntervalDays, FertilizingIntervalDays, TargetHumidityMin, TargetHumidityMax,
       TargetTemperatureMin, TargetTemperatureMax, PersonalCareNotes,
       ActualPlacement, ActualLight, HasRainCover, SubstrateType, SaucerState, City,
       OverrideSuggestedLight, OverrideCareTaboosJson,
       WateringIntervalDetachedFromWiki, EnvironmentMismatchAcknowledged, AiEnvironmentAdvice
FROM dbo.PlantProfile WHERE Deleted = 0";

    public async Task<PlantProfileModel?> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        using var conn = Conn();
        return await conn.QueryFirstOrDefaultAsync<PlantProfileModel>(new CommandDefinition(
            SelectSql + " AND PlantID = @PlantId", new { PlantId = plantId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyDictionary<Guid, PlantProfileModel>> GetByPlantIdsAsync(
        IReadOnlyCollection<Guid> plantIds,
        CancellationToken cancellationToken = default)
    {
        if (plantIds.Count == 0)
            return new Dictionary<Guid, PlantProfileModel>();

        using var conn = Conn();
        var rows = await conn.QueryAsync<PlantProfileModel>(new CommandDefinition(
            SelectSql + " AND PlantID IN @PlantIds",
            new { PlantIds = plantIds.ToArray() },
            cancellationToken: cancellationToken));
        return rows.GroupBy(r => r.PlantID).ToDictionary(g => g.Key, g => g.First());
    }

    public async Task InsertAsync(PlantProfileModel profile, CancellationToken cancellationToken = default)
    {
        profile.ID = profile.ID == Guid.Empty ? Guid.NewGuid() : profile.ID;
        var now = DateTime.UtcNow;
        profile.CreateDate = profile.CreateDate == default ? now : profile.CreateDate;
        profile.ModifyDate = now;
        profile.Enabled = true;
        profile.Deleted = false;

        const string sql = @"
INSERT INTO dbo.PlantProfile
    (ID, CreateDate, ModifyDate, Enabled, Deleted, PlantID,
     WateringIntervalDays, FertilizingIntervalDays, TargetHumidityMin, TargetHumidityMax,
     TargetTemperatureMin, TargetTemperatureMax, PersonalCareNotes,
     ActualPlacement, ActualLight, HasRainCover, SubstrateType, SaucerState, City,
     OverrideSuggestedLight, OverrideCareTaboosJson,
     WateringIntervalDetachedFromWiki, EnvironmentMismatchAcknowledged, AiEnvironmentAdvice)
VALUES
    (@ID, @CreateDate, @ModifyDate, @Enabled, @Deleted, @PlantID,
     @WateringIntervalDays, @FertilizingIntervalDays, @TargetHumidityMin, @TargetHumidityMax,
     @TargetTemperatureMin, @TargetTemperatureMax, @PersonalCareNotes,
     @ActualPlacement, @ActualLight, @HasRainCover, @SubstrateType, @SaucerState, @City,
     @OverrideSuggestedLight, @OverrideCareTaboosJson,
     @WateringIntervalDetachedFromWiki, @EnvironmentMismatchAcknowledged, @AiEnvironmentAdvice);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        // 不可直接傳 profile：Id/PlantId 相容屬性會與 ID/PlantID 在 SQL 參數名撞名（不分大小寫）
        using var conn = Conn();
        profile.Seq = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, DbParams.From(profile), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(PlantProfileModel profile, CancellationToken cancellationToken = default)
    {
        profile.ModifyDate = DateTime.UtcNow;
        const string sql = @"
UPDATE dbo.PlantProfile SET
    ModifyDate = @ModifyDate, Enabled = @Enabled,
    WateringIntervalDays = @WateringIntervalDays, FertilizingIntervalDays = @FertilizingIntervalDays,
    TargetHumidityMin = @TargetHumidityMin, TargetHumidityMax = @TargetHumidityMax,
    TargetTemperatureMin = @TargetTemperatureMin, TargetTemperatureMax = @TargetTemperatureMax,
    PersonalCareNotes = @PersonalCareNotes, ActualPlacement = @ActualPlacement, ActualLight = @ActualLight,
    HasRainCover = @HasRainCover, SubstrateType = @SubstrateType, SaucerState = @SaucerState, City = @City,
    OverrideSuggestedLight = @OverrideSuggestedLight, OverrideCareTaboosJson = @OverrideCareTaboosJson,
    WateringIntervalDetachedFromWiki = @WateringIntervalDetachedFromWiki,
    EnvironmentMismatchAcknowledged = @EnvironmentMismatchAcknowledged, AiEnvironmentAdvice = @AiEnvironmentAdvice
WHERE ID = @ID AND Deleted = 0";

        using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(sql, DbParams.From(profile), cancellationToken: cancellationToken));
    }
}
