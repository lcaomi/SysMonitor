using System.Diagnostics;
using PerfMonitor.App.Models;

namespace PerfMonitor.App.Metrics;

public class NetworkMetricProvider : IMetricProvider, IDisposable
{
    public string Name => "NET";

    private PerformanceCounter? _sentCounter;
    private PerformanceCounter? _receivedCounter;
    private bool _initialized;

    public NetworkMetricProvider()
    {
        try
        {
            var category = new PerformanceCounterCategory("Network Interface");
            var instanceNames = category.GetInstanceNames();

            // Pick the first active interface
            var instance = instanceNames.FirstOrDefault(n =>
                !n.Contains("isatap", StringComparison.OrdinalIgnoreCase) &&
                !n.Contains("teredo", StringComparison.OrdinalIgnoreCase) &&
                !n.Contains("pseudo", StringComparison.OrdinalIgnoreCase))
                ?? instanceNames.FirstOrDefault();

            if (instance is null)
            {
                _initialized = false;
                return;
            }

            _sentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
            _receivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);
            _sentCounter.NextValue();
            _receivedCounter.NextValue();
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    public Task<MetricSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!_initialized || _sentCounter is null || _receivedCounter is null)
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
            var sent = _sentCounter.NextValue();
            var received = _receivedCounter.NextValue();

            return Task.FromResult(new MetricSnapshot
            {
                Name = Name,
                Value = $"▲ {FormatBytes(sent)}  ▼ {FormatBytes(received)}",
                Detail = $"Up:{FormatBytes(sent)}/s Down:{FormatBytes(received)}/s",
                NumericValue = Math.Max(sent, received) / 1_000_000f * 100,
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

    private static string FormatBytes(float bytesPerSec)
    {
        return bytesPerSec switch
        {
            >= 1_000_000 => $"{bytesPerSec / 1_000_000:F1} MB",
            >= 1_000 => $"{bytesPerSec / 1_000:F0} KB",
            _ => $"{bytesPerSec:F0} B"
        };
    }

    public void Dispose()
    {
        _sentCounter?.Dispose();
        _receivedCounter?.Dispose();
    }
}
