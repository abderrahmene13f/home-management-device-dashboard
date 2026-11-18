using HomeManagementDashboard;
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<DeviceOptions>(builder.Configuration.GetSection("Devices"));
builder.Services.AddHttpClient("devices", c => c.Timeout = TimeSpan.FromSeconds(4));
builder.Services.AddSingleton<DeviceRepository>();
builder.Services.AddHostedService<DevicePollingService>();
var app = builder.Build();
await app.Services.GetRequiredService<DeviceRepository>().InitializeAsync();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/dashboard", async (DeviceRepository r, CancellationToken ct) => Results.Ok(await r.GetDashboardAsync(ct)));
app.MapGet("/api/devices", async (DeviceRepository r, CancellationToken ct) => Results.Ok(await r.GetDevicesAsync(ct)));
app.MapPost("/api/devices/{id}/refresh", async (string id, DevicePollingService p, CancellationToken ct) =>
    await p.RefreshOneAsync(id, ct) ? Results.Ok() : Results.NotFound());
app.MapGet("/api/devices/{id}/history", async (string id, DeviceRepository r, CancellationToken ct) =>
    Results.Ok(await r.GetHistoryAsync(id, 50, ct)));
app.Run();
