namespace HomeManagementDashboard;
public sealed class DeviceOptions {
    public List<DeviceDefinition> Items { get; set; } = [];
    public int OnlineThresholdSeconds { get; set; } = 60;
    public int PollIntervalSeconds { get; set; } = 15;
}
public sealed class DeviceDefinition {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string InfoUrl { get; set; } = "";
}
public sealed record DeviceView(string Id, string Name, string Url, bool IsOnline,
    DateTimeOffset? LastSeenUtc, string? Address, Dictionary<string,string> Metrics, string? Error);
public sealed record DashboardView(int TotalDevices, int OnlineDevices, int OfflineDevices,
    DateTimeOffset UpdatedAtUtc, IReadOnlyList<DeviceView> Devices);
public sealed record DeviceSnapshot(long Id, string DeviceId, DateTimeOffset SeenAtUtc,
    bool IsOnline, string? Name, string? Address, string MetricsJson, string? Error);
