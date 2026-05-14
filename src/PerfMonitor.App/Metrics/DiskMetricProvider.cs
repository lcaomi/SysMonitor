using System.Diagnostics;
using PerfMonitor.App.Models;

namespace PerfMonitor.App.Metrics;

public class DiskMetricProvider : IMetricProvider, IDisposable
{
    public string Name => "DISK";

    private PerformanceCounter? _readCounter;
    private PerformanceCounter? _writeCounter;
    private bool _initialized;

    public DiskMetricProvider()
    {
        try
        {
            _readCounter = new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", "_Total");
            _writeCounter = new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", "_Total");
            _readCounter.NextValue();
            _writeCounter.NextValue();
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    public Task<MetricSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!_initialized || _readCounter is null || _writeCounter is null)
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
            var readBytes = _readCounter.NextValue();
            var writeBytes = _writeCounter.NextValue();

            return Task.FromResult(new MetricSnapshot
            {
                Name = Name,
                Value = $"R {FormatBytes(readBytes)}  W {FormatBytes(writeBytes)}",
                Detail = $"R:{FormatBytes(readBytes)}/s W:{FormatBytes(writeBytes)}/s",
                NumericValue = Math.Max(readBytes, writeBytes) / 1_000_000f * 100, // Scale for sparkline
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
        _readCounter?.Dispose();
        _writeCounter?.Dispose();
    }
}
