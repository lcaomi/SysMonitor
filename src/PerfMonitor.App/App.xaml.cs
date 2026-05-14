using System.Diagnostics;
using System.Windows;
using PerfMonitor.App.Metrics;
using PerfMonitor.App.Services;

namespace PerfMonitor.App;

public partial class App : Application
{
    private static readonly string MutexName = "WindowsPerfMonitor_SingleInstance";
    private Mutex? _mutex;
    private TrayService? _trayService;
    private MainWindow? _mainWindow;
    private SettingsService? _settingsService;
    private CpuMetricProvider? _cpuProvider;
    private MemoryMetricProvider? _memoryProvider;
    private System.Windows.Forms.Timer? _tooltipTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance check
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }

        // Load settings
        _settingsService = new SettingsService();
        var settings = _settingsService.Load();

        // Create tray service
        _trayService = new TrayService(settings, _settingsService);
        _trayService.ShowWindowRequested += OnShowWindow;
        _trayService.ExitRequested += OnExit;
        _trayService.ToggleTopmostRequested += OnToggleTopmost;
        _trayService.ToggleAutoTransparencyRequested += OnToggleAutoTransparency;

        // Create main window
        _mainWindow = new MainWindow(settings, _settingsService);
        _trayService.SetMainWindow(_mainWindow);

        // Start tray tooltip update timer
        _cpuProvider = new CpuMetricProvider();
        _memoryProvider = new MemoryMetricProvider();
        _tooltipTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _tooltipTimer.Tick += OnTooltipTimerTick;
        _tooltipTimer.Start();

        // Show the window
        _mainWindow.Show();

        // Handle session ending
        SessionEnding += (_, _) => Cleanup();
        Exit += (_, _) => Cleanup();
    }

    private async void OnTooltipTimerTick(object? sender, EventArgs e)
    {
        if (_trayService is null || _cpuProvider is null || _memoryProvider is null)
            return;

        try
        {
            var cpuSnapshot = await _cpuProvider.GetSnapshotAsync(CancellationToken.None);
            var memSnapshot = await _memoryProvider.GetSnapshotAsync(CancellationToken.None);

            var tooltip = $"CPU: {cpuSnapshot.Value}  MEM: {memSnapshot.Detail ?? memSnapshot.Value}";
            _trayService.UpdateTooltip(tooltip);
        }
        catch
        {
            // Ignore tooltip update failures
        }
    }

    private void OnShowWindow()
    {
        if (_mainWindow is null) return;

        if (_mainWindow.IsVisible)
            _mainWindow.HideWindow();
        else
            _mainWindow.ShowWindow();
    }

    private void OnToggleTopmost()
    {
        _mainWindow?.ToggleTopmost();
        _trayService?.UpdateContextMenu();
    }

    private void OnToggleAutoTransparency()
    {
        _mainWindow?.ToggleAutoTransparency();
        _trayService?.UpdateContextMenu();
    }

    private void OnExit()
    {
        _mainWindow?.ForceClose();
        Cleanup();
        Shutdown();
    }

    private void Cleanup()
    {
        _tooltipTimer?.Stop();
        _tooltipTimer?.Dispose();

        _cpuProvider?.Dispose();
        _memoryProvider?.Dispose();

        _trayService?.Dispose();

        _mutex?.Dispose();
        _mutex = null;
    }
}
