using System.Diagnostics;
using PerfMonitor.App.Models;

namespace PerfMonitor.App.Metrics;

public class MemoryMetricProvider : IMetricProvider, IDisposable
{
    public string Name => "MEM";

    private PerformanceCounter? _availableCounter;
    private bool _initialized;

    public MemoryMetricProvider()
    {
        try
        {
            _availableCounter = new PerformanceCounter("Memory", "Available MBytes");
            _availableCounter.NextValue();
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    public Task<MetricSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!_initialized || _availableCounter is null)
        {
            return Task.FromResult(new MetricSnapshot
            {
                Name = Name,
                Value = "N/A",
                IsAvailable = false
            });
        }

        try
        {
            var availableMB = _availableCounter.NextValue();
            var totalMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0);
            var usedMB = totalMB - availableMB;
            var percent = usedMB / totalMB * 100;

            return Task.FromResult(new MetricSnapshot
            {
                Name = Name,
                Value = $"{usedMB / 1024:F1} / {totalMB / 1024:F1} GB  {percent:F0}%",
                Detail = $"{percent:F0}%",
                IsAvailable = true
            });
        }
        catch
        {
            return Task.FromResult(new MetricSnapshot
            {
                Name = Name,
                Value = "N/A",
                IsAvailable = false
            });
        }
    }

    public void Dispose()
    {
        _availableCounter?.Dispose();
    }
}
