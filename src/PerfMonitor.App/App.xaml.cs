using System.Diagnostics;
using System.Windows;
using PerfMonitor.App.Metrics;
using PerfMonitor.App.Models;
using PerfMonitor.App.Services;

namespace PerfMonitor.App;

public partial class App : Application
{
    private static readonly string MutexName = "WindowsPerfMonitor_SingleInstance";
    private Mutex? _mutex;
    private TrayService? _trayService;
    private MainWindow? _mainWindow;
    private SettingsService? _settingsService;
    private AppSettings _settings = new();
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
        _settings = _settingsService.Load();

        // Create tray service
        _trayService = new TrayService(_settings, _settingsService);
        _trayService.ShowWindowRequested += OnShowWindow;
        _trayService.ExitRequested += OnExit;
        _trayService.ToggleTopmostRequested += OnToggleTopmost;
        _trayService.ToggleAutoTransparencyRequested += OnToggleAutoTransparency;
        _trayService.SettingsRequested += OnSettings;
        _trayService.TogglePassthroughRequested += OnTogglePassthrough;
        _trayService.ToggleStartWithWindowsRequested += OnToggleStartWithWindows;

        // Create main window
        _mainWindow = new MainWindow(_settings, _settingsService);
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
            _trayService.UpdateTooltip($"CPU: {cpuSnapshot.Value}  MEM: {memSnapshot.Detail ?? memSnapshot.Value}");
        }
        catch { }
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

    private void OnSettings()
    {
        _mainWindow?.Dispatcher.Invoke(() =>
        {
            if (_mainWindow.IsVisible)
                _mainWindow.ShowWindow();

            // Trigger settings from main window
            var settingsWin = new Views.SettingsWindow(_settings, _settingsService!);
            settingsWin.Owner = _mainWindow;
            settingsWin.ShowDialog();

            if (settingsWin.Saved)
            {
                _mainWindow.ReloadMetrics();
                _trayService?.UpdateContextMenu();
            }
        });
    }

    private void OnTogglePassthrough()
    {
        _mainWindow?.ToggleMousePassthrough();
        _trayService?.SetPassthroughChecked(_mainWindow?.IsMousePassthroughEnabled() ?? false);
    }

    private void OnToggleStartWithWindows()
    {
        var isEnabled = StartupService.IsAutoStartEnabled();
        StartupService.SetAutoStart(!isEnabled);
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
