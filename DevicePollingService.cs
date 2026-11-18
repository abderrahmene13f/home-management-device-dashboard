using System.Text.Json;
using Microsoft.Extensions.Options;
namespace HomeManagementDashboard;
public sealed class DevicePollingService(IHttpClientFactory factory,IOptions<DeviceOptions> opts,
    DeviceRepository repo,ILogger<DevicePollingService> logger) : BackgroundService {
    private readonly DeviceOptions options=opts.Value;
    protected override async Task ExecuteAsync(CancellationToken stop) {
        await PollAll(stop); using var timer=new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(options.PollIntervalSeconds,5,300)));
        while(await timer.WaitForNextTickAsync(stop)) await PollAll(stop);
    }
    public async Task<bool> RefreshOneAsync(string id,CancellationToken ct) {
        var d=options.Items.FirstOrDefault(x=>string.Equals(x.Id,id,StringComparison.OrdinalIgnoreCase));
        return d!=null && await PollOne(d,ct);
    }
    private async Task PollAll(CancellationToken ct) { foreach(var d in options.Items) { if(ct.IsCancellationRequested) break; await PollOne(d,ct); } }
    private async Task<bool> PollOne(DeviceDefinition d,CancellationToken ct) {
        var now=DateTimeOffset.UtcNow;
        try {
            var client=factory.CreateClient("devices"); using var response=await client.GetAsync(d.InfoUrl,ct);
            response.EnsureSuccessStatusCode(); using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root=json.RootElement; string? name=Get(root,"name"), address=Get(root,"address");
            var metrics=new Dictionary<string,string>();
            if(root.TryGetProperty("metrics",out var obj)&&obj.ValueKind==JsonValueKind.Object)
                foreach(var p in obj.EnumerateObject()) metrics[p.Name]=p.Value.ValueKind==JsonValueKind.String?p.Value.GetString()??"":p.Value.ToString();
            await repo.SaveAsync(new(0,d.Id,now,true,name??d.Name,address,JsonSerializer.Serialize(metrics),null),ct); return true;
        } catch(Exception ex) when(ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning("Device {Id} poll failed: {Message}",d.Id,ex.Message);
            await repo.SaveAsync(new(0,d.Id,now,false,d.Name,null,"{}",ex.Message),ct); return false;
        }
    }
    private static string? Get(JsonElement e,string p)=>e.TryGetProperty(p,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():null;
}
