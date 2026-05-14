using PerfMonitor.App.Models;

namespace PerfMonitor.App.Metrics;

public interface IMetricProvider
{
    string Name { get; }
    Task<MetricSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
