using System.Diagnostics;
using PerfMonitor.App.Models;

namespace PerfMonitor.App.Metrics;

public class CpuMetricProvider : IMetricProvider, IDisposable
{
    public string Name => "CPU";

    private PerformanceCounter? _counter;
    private bool _initialized;

    public CpuMetricProvider()
    {
        try
        {
            _counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _counter.NextValue(); // Discard first reading
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    public Task<MetricSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!_initialized || _counter is null)
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
            var usage = _counter.NextValue();
            return Task.FromResult(new MetricSnapshot
            {
                Name = Name,
                Value = $"{usage:F1}%",
                Detail = $"{usage:F0}%",
                NumericValue = usage,
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
        _counter?.Dispose();
    }
}
