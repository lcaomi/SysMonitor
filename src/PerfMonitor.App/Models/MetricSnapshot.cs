namespace PerfMonitor.App.Models;

public class MetricSnapshot
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Detail { get; init; }
    public bool IsAvailable { get; init; } = true;
}
