using System.Management;
using PerfMonitor.App.Models;

namespace PerfMonitor.App.Metrics;

public class TemperatureMetricProvider : IMetricProvider
{
    public string Name => "TEMP";

    private bool _initialized;
    private ManagementObject? _thermalZone;

    public TemperatureMetricProvider()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT * FROM MSAcpi_ThermalZoneTemperature");

            _thermalZone = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
            _initialized = _thermalZone is not null;
        }
        catch
        {
            _initialized = false;
        }
    }

    public Task<MetricSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!_initialized || _thermalZone is null)
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
            var tempKelvin = Convert.ToDouble(_thermalZone.GetPropertyValue("CurrentTemperature"));
            var tempCelsius = (tempKelvin / 10.0) - 273.15;

            return Task.FromResult(new MetricSnapshot
            {
                Name = Name,
                Value = $"{tempCelsius:F0}°C",
                Detail = $"{tempCelsius:F0}°C",
                NumericValue = tempCelsius,
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
}
