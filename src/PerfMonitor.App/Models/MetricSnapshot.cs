namespace PerfMonitor.App.Models;

public class MetricSnapshot
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Detail { get; init; }
    public bool IsAvailable { get; init; } = true;
    /// <summary>Numeric representation for sparkline charting (0-100 or absolute).</summary>
    public double NumericValue { get; init; }
}
