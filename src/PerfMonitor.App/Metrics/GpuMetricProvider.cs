using System.Diagnostics;
using PerfMonitor.App.Models;

namespace PerfMonitor.App.Metrics;

public class GpuMetricProvider : IMetricProvider, IDisposable
{
    public string Name => "GPU";

    private List<PerformanceCounter> _gpuCounters = [];
    private bool _initialized;

    public GpuMetricProvider()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames()
                .Where(n => n.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var name in instanceNames)
            {
                try
                {
                    var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", name);
                    counter.NextValue();
                    _gpuCounters.Add(counter);
                }
                catch { }
            }

            _initialized = _gpuCounters.Count > 0;
        }
        catch
        {
            _initialized = false;
        }
    }

    public Task<MetricSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!_initialized || _gpuCounters.Count == 0)
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
            var totalUsage = 0f;
            foreach (var counter in _gpuCounters)
            {
                totalUsage += counter.NextValue();
            }

            var avgUsage = totalUsage / _gpuCounters.Count;
            var memoryText = TryGetMemoryInfo();

            return Task.FromResult(new MetricSnapshot
            {
                Name = Name,
                Value = memoryText is not null
                    ? $"{avgUsage:F0}%  {memoryText}"
                    : $"{avgUsage:F0}%",
                Detail = $"{avgUsage:F0}%",
                NumericValue = avgUsage,
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

    private static string? TryGetMemoryInfo()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=memory.used,memory.total --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(1000);

            if (!string.IsNullOrEmpty(output))
            {
                var parts = output.Split(',');
                if (parts.Length >= 2 &&
                    float.TryParse(parts[0].Trim(), out var used) &&
                    float.TryParse(parts[1].Trim(), out var total))
                {
                    return $"{used / 1024:F1}G";
                }
            }
        }
        catch { }

        return null;
    }

    public void Dispose()
    {
        foreach (var counter in _gpuCounters)
            counter.Dispose();
        _gpuCounters.Clear();
    }
}
