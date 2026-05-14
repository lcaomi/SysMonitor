using System.Windows;
using PerfMonitor.App.Models;
using PerfMonitor.App.Services;

namespace PerfMonitor.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    public bool Saved { get; private set; }

    public SettingsWindow(AppSettings settings, SettingsService settingsService)
    {
        InitializeComponent();
        _settings = settings;
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Appearance
        ThemeCombo.SelectedIndex = _settings.Appearance.Theme == "dark" ? 0 : 1;
        AutoTransparencyCheck.IsChecked = _settings.Appearance.AutoTransparency;
        NormalOpacitySlider.Value = _settings.Appearance.NormalOpacity;
        IdleOpacitySlider.Value = _settings.Appearance.IdleOpacity;
        CompactModeCheck.IsChecked = _settings.Appearance.CompactMode;

        // Metrics
        RefreshIntervalCombo.SelectedValue = _settings.Metrics.RefreshIntervalMs.ToString();
        ShowCpuCheck.IsChecked = _settings.Metrics.ShowCpu;
        ShowMemoryCheck.IsChecked = _settings.Metrics.ShowMemory;
        ShowDiskCheck.IsChecked = _settings.Metrics.ShowDisk;
        ShowNetworkCheck.IsChecked = _settings.Metrics.ShowNetwork;
        ShowGpuCheck.IsChecked = _settings.Metrics.ShowGpu;
        ShowTemperatureCheck.IsChecked = _settings.Metrics.ShowTemperature;

        // Behavior
        TopmostCheck.IsChecked = _settings.Window.Topmost;
        CloseToTrayCheck.IsChecked = _settings.Behavior.CloseToTray;
        StartWithWindowsCheck.IsChecked = StartupService.IsAutoStartEnabled();
        MousePassthroughCheck.IsChecked = _settings.Behavior.SingleInstance; // Reuse existing flag or add new one

        // Load mouse passthrough state (stored in Window settings for now)
        MousePassthroughCheck.IsChecked = _settings.Window.ShowInTaskbar == false
            ? false  // default off
            : false;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Appearance
        _settings.Appearance.Theme = ThemeCombo.SelectedIndex == 0 ? "dark" : "light";
        _settings.Appearance.AutoTransparency = AutoTransparencyCheck.IsChecked == true;
        _settings.Appearance.NormalOpacity = NormalOpacitySlider.Value;
        _settings.Appearance.IdleOpacity = IdleOpacitySlider.Value;
        _settings.Appearance.CompactMode = CompactModeCheck.IsChecked == true;

        // Metrics
        if (int.TryParse(RefreshIntervalCombo.Text, out var interval))
            _settings.Metrics.RefreshIntervalMs = interval;

        _settings.Metrics.ShowCpu = ShowCpuCheck.IsChecked == true;
        _settings.Metrics.ShowMemory = ShowMemoryCheck.IsChecked == true;
        _settings.Metrics.ShowDisk = ShowDiskCheck.IsChecked == true;
        _settings.Metrics.ShowNetwork = ShowNetworkCheck.IsChecked == true;
        _settings.Metrics.ShowGpu = ShowGpuCheck.IsChecked == true;
        _settings.Metrics.ShowTemperature = ShowTemperatureCheck.IsChecked == true;

        // Behavior
        _settings.Window.Topmost = TopmostCheck.IsChecked == true;
        _settings.Behavior.CloseToTray = CloseToTrayCheck.IsChecked == true;
        StartupService.SetAutoStart(StartWithWindowsCheck.IsChecked == true);

        _settingsService.Save(_settings);
        Saved = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Saved = false;
        Close();
    }
}
