namespace PerfMonitor.App.Models;

public class WindowSettings
{
    public double Left { get; set; } = 1200;
    public double Top { get; set; } = 80;
    public double Width { get; set; } = 280;
    public double Height { get; set; } = 200;
    public bool Topmost { get; set; } = true;
    public bool ShowInTaskbar { get; set; } = false;
}

public class AppearanceSettings
{
    public string Theme { get; set; } = "dark";
    public double NormalOpacity { get; set; } = 0.95;
    public double IdleOpacity { get; set; } = 0.35;
    public bool AutoTransparency { get; set; } = true;
    public int FontSize { get; set; } = 13;
    public bool CompactMode { get; set; } = false;
}

public class MetricsSettings
{
    public int RefreshIntervalMs { get; set; } = 1000;
    public bool ShowCpu { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public bool ShowDisk { get; set; } = true;
    public bool ShowNetwork { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowTemperature { get; set; } = false;
}

public class BehaviorSettings
{
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public bool SingleInstance { get; set; } = true;
}

public class AppSettings
{
    public WindowSettings Window { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public MetricsSettings Metrics { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
}
