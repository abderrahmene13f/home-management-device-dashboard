using Microsoft.Data.Sqlite;
using System.Text.Json;
namespace HomeManagementDashboard;
public sealed class DeviceRepository(IConfiguration configuration) {
    private readonly string cs = new SqliteConnectionStringBuilder {
        DataSource = configuration["Database:Path"] ?? "data/devices.db"
    }.ToString();
    private async Task<SqliteConnection> Open(CancellationToken ct) {
        var db = new SqliteConnection(cs); await db.OpenAsync(ct); return db;
    }
    public async Task InitializeAsync() {
        var path = new SqliteConnectionStringBuilder(cs).DataSource;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await using var db = await Open(CancellationToken.None);
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS DeviceSnapshots (Id INTEGER PRIMARY KEY AUTOINCREMENT, DeviceId TEXT NOT NULL, SeenAtUtc TEXT NOT NULL, IsOnline INTEGER NOT NULL, Name TEXT NULL, Address TEXT NULL, MetricsJson TEXT NOT NULL, Error TEXT NULL); CREATE INDEX IF NOT EXISTS IX_DeviceSnapshots_DeviceId_SeenAtUtc ON DeviceSnapshots(DeviceId, SeenAtUtc DESC);";
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task SaveAsync(DeviceSnapshot s, CancellationToken ct) {
        await using var db = await Open(ct); await using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO DeviceSnapshots(DeviceId,SeenAtUtc,IsOnline,Name,Address,MetricsJson,Error) VALUES($id,$time,$online,$name,$address,$metrics,$error)";
        cmd.Parameters.AddWithValue("$id",s.DeviceId); cmd.Parameters.AddWithValue("$time",s.SeenAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$online",s.IsOnline ? 1 : 0); cmd.Parameters.AddWithValue("$name",(object?)s.Name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$address",(object?)s.Address ?? DBNull.Value); cmd.Parameters.AddWithValue("$metrics",s.MetricsJson);
        cmd.Parameters.AddWithValue("$error",(object?)s.Error ?? DBNull.Value); await cmd.ExecuteNonQueryAsync();
    }
    public async Task<IReadOnlyList<DeviceView>> GetDevicesAsync(CancellationToken ct) {
        var defs = configuration.GetSection("Devices:Items").Get<List<DeviceDefinition>>() ?? [];
        var result = new List<DeviceView>(); await using var db = await Open(ct);
        foreach(var d in defs) {
            await using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT SeenAtUtc,IsOnline,Name,Address,MetricsJson,Error FROM DeviceSnapshots WHERE DeviceId=$id ORDER BY SeenAtUtc DESC LIMIT 1";
            cmd.Parameters.AddWithValue("$id",d.Id); await using var rd = await cmd.ExecuteReaderAsync(ct);
            if(await rd.ReadAsync(ct)) {
                var metrics = JsonSerializer.Deserialize<Dictionary<string,string>>(rd.GetString(4)) ?? [];
                result.Add(new(d.Id,rd.IsDBNull(2)?d.Name:rd.GetString(2),d.InfoUrl,rd.GetInt32(1)==1,
                    DateTimeOffset.Parse(rd.GetString(0)),rd.IsDBNull(3)?null:rd.GetString(3),metrics,rd.IsDBNull(5)?null:rd.GetString(5)));
            } else result.Add(new(d.Id,d.Name,d.InfoUrl,false,null,null,[],"No successful check yet."));
        }
        return result;
    }
    public async Task<DashboardView> GetDashboardAsync(CancellationToken ct) {
        var list = await GetDevicesAsync(ct); var now = DateTimeOffset.UtcNow;
        var threshold = configuration.GetValue("Devices:OnlineThresholdSeconds",60);
        var current = list.Select(d => d with { IsOnline = d.LastSeenUtc.HasValue && now-d.LastSeenUtc.Value <= TimeSpan.FromSeconds(threshold) }).ToList();
        return new(current.Count,current.Count(x=>x.IsOnline),current.Count(x=>!x.IsOnline),now,current);
    }
    public async Task<IReadOnlyList<DeviceSnapshot>> GetHistoryAsync(string id,int limit,CancellationToken ct) {
        var rows = new List<DeviceSnapshot>(); await using var db = await Open(ct); await using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Id,DeviceId,SeenAtUtc,IsOnline,Name,Address,MetricsJson,Error FROM DeviceSnapshots WHERE DeviceId=$id ORDER BY SeenAtUtc DESC LIMIT $limit";
        cmd.Parameters.AddWithValue("$id",id); cmd.Parameters.AddWithValue("$limit",Math.Clamp(limit,1,200));
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while(await rd.ReadAsync(ct)) rows.Add(new(rd.GetInt64(0),rd.GetString(1),DateTimeOffset.Parse(rd.GetString(2)),rd.GetInt32(3)==1,
            rd.IsDBNull(4)?null:rd.GetString(4),rd.IsDBNull(5)?null:rd.GetString(5),rd.GetString(6),rd.IsDBNull(7)?null:rd.GetString(7)));
        return rows;
    }
}
