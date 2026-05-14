using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PerfMonitor.App.Metrics;
using PerfMonitor.App.Models;
using PerfMonitor.App.Services;
using PerfMonitor.App.Views;

namespace PerfMonitor.App;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly List<IMetricProvider> _providers = [];
    private readonly Dictionary<string, Sparkline> _sparklines = [];
    private readonly Dictionary<string, TextBlock> _valueLabels = [];
    private readonly Dictionary<string, Grid> _metricRows = [];
    private PeriodicTimer? _refreshTimer;
    private readonly CancellationTokenSource _cts = new();

    // Auto-transparency
    private readonly DispatcherTimer _transparencyTimer;
    private bool _autoTransparencyEnabled;
    private double _normalOpacity;
    private double _idleOpacity;

    // Window dragging
    private bool _isDragging;
    private Point _dragStartPoint;

    // Mouse passthrough
    private bool _mousePassthrough;

    // P/Invoke for mouse passthrough
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int style);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_LAYERED = 0x80000;

    public MainWindow(AppSettings settings, SettingsService settingsService)
    {
        InitializeComponent();

        _settings = settings;
        _settingsService = settingsService;

        LoadSettings();
        BuildMetricRows();
        StartSampling();

        // Auto-transparency timer
        _transparencyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _transparencyTimer.Tick += OnTransparencyTimerTick;

        Closing += OnWindowClosing;
        Closed += (_, _) => Cleanup();
        LocationChanged += OnLocationChanged;
    }

    #region Settings & Row Building

    private void LoadSettings()
    {
        var win = _settings.Window;
        Left = win.Left;
        Top = win.Top;
        Width = win.Width;
        Height = win.Height;
        Topmost = win.Topmost;
        ShowInTaskbar = win.ShowInTaskbar;

        var app = _settings.Appearance;
        _normalOpacity = app.NormalOpacity;
        _idleOpacity = app.IdleOpacity;
        _autoTransparencyEnabled = app.AutoTransparency;
        Opacity = _normalOpacity;

        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var isDark = _settings.Appearance.Theme == "dark";
        var bg = isDark ? Color.FromRgb(0x16, 0x1C, 0x28) : Color.FromRgb(0xE8, 0xE8, 0xF0);
        var border = isDark
            ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x44, 0x00, 0x00, 0x00);

        MainBorder.Background = new SolidColorBrush(Color.FromArgb(0xE8, bg.R, bg.G, bg.B));
        MainBorder.BorderBrush = new SolidColorBrush(border);
    }

    private void BuildMetricRows()
    {
        _providers.Clear();
        _metricRows.Clear();
        _sparklines.Clear();
        _valueLabels.Clear();
        MetricsPanel.Children.Clear();

        // Provider definitions with metadata
        var definitions = new (string Key, string Icon, string ColorHex, Func<IMetricProvider> Factory)[]
        {
            ("CPU", "", "#FF00B7C3", () => new CpuMetricProvider()),
            ("MEM", "", "#FFFFB900", () => new MemoryMetricProvider()),
            ("DISK", "", "#FF44D7B6", () => new DiskMetricProvider()),
            ("NET", "", "#FF4DB8FF", () => new NetworkMetricProvider()),
            ("GPU", "", "#FFE06C75", () => new GpuMetricProvider()),
            ("TEMP", "", "#FFFF8C42", () => new TemperatureMetricProvider()),
        };

        // Visibility config
        var visibility = new Dictionary<string, bool>
        {
            ["CPU"] = _settings.Metrics.ShowCpu,
            ["MEM"] = _settings.Metrics.ShowMemory,
            ["DISK"] = _settings.Metrics.ShowDisk,
            ["NET"] = _settings.Metrics.ShowNetwork,
            ["GPU"] = _settings.Metrics.ShowGpu,
            ["TEMP"] = _settings.Metrics.ShowTemperature,
        };

        var rowIndex = 0;
        foreach (var def in definitions)
        {
            if (!visibility.GetValueOrDefault(def.Key, true))
                continue;

            var provider = def.Factory();
            _providers.Add(provider);

            var sparkline = new Sparkline
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(def.ColorHex)),
                Margin = new Thickness(0, 0, 6, 0)
            };
            _sparklines[def.Key] = sparkline;

            var valueLabel = new TextBlock
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
                FontSize = _settings.Appearance.FontSize,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Text = "--"
            };
            _valueLabels[def.Key] = valueLabel;

            var row = new Grid { Margin = new System.Windows.Thickness(0, _settings.Appearance.CompactMode ? 1 : 4, 0, _settings.Appearance.CompactMode ? 1 : 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });    // Icon
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });     // Name
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });     // Sparkline
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Value

            // Icon
            var icon = new TextBlock
            {
                Text = def.Icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(def.ColorHex)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            row.Children.Add(icon);

            // Name label
            var nameLabel = new TextBlock
            {
                Text = def.Key,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCFFFFFF")),
                FontSize = _settings.Appearance.FontSize,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            Grid.SetColumn(nameLabel, 1);
            row.Children.Add(nameLabel);

            // Sparkline
            Grid.SetColumn(sparkline, 2);
            row.Children.Add(sparkline);

            // Value
            Grid.SetColumn(valueLabel, 3);
            row.Children.Add(valueLabel);

            _metricRows[def.Key] = row;
            MetricsPanel.Children.Add(row);
            rowIndex++;
        }

        // Adjust window height based on visible rows
        var rowHeight = _settings.Appearance.CompactMode ? 24 : 32;
        var totalHeight = 48 + rowIndex * rowHeight;
        if (rowIndex > 0)
        {
            Height = Math.Max(totalHeight, 120);
        }
    }

    private void StartSampling()
    {
        _refreshTimer?.Dispose();
        var interval = TimeSpan.FromMilliseconds(_settings.Metrics.RefreshIntervalMs);
        _refreshTimer = new PeriodicTimer(interval);
        _ = RunSamplingLoopAsync(_cts.Token);
    }

    public void ReloadMetrics()
    {
        // Dispose old providers
        foreach (var p in _providers)
            if (p is IDisposable d) d.Dispose();
        _providers.Clear();

        BuildMetricRows();
        StartSampling();
        ApplyTheme();

        foreach (var row in _metricRows.Values)
        {
            if (row.Children[2] is Sparkline s)
                s.Reset();
        }
        foreach (var label in _valueLabels.Values)
            label.Text = "--";
    }

    #endregion

    #region Sampling Loop

    private async Task RunSamplingLoopAsync(CancellationToken ct)
    {
        if (_refreshTimer is null) return;
        while (await _refreshTimer.WaitForNextTickAsync(ct))
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var snapshots = new List<MetricSnapshot>();
                foreach (var provider in _providers)
                {
                    var snapshot = await provider.GetSnapshotAsync(ct);
                    snapshots.Add(snapshot);
                }

                await Dispatcher.InvokeAsync(() => UpdateUI(snapshots));
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private void UpdateUI(List<MetricSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (_valueLabels.TryGetValue(snapshot.Name, out var label))
            {
                label.Text = snapshot.IsAvailable ? snapshot.Value : "N/A";
                label.Foreground = snapshot.IsAvailable
                    ? new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF))
                    : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            }

            if (_sparklines.TryGetValue(snapshot.Name, out var sparkline))
            {
                sparkline.AddValue(snapshot.IsAvailable ? snapshot.NumericValue : 0);
            }
        }
    }

    #endregion

    #region Auto Transparency

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_mousePassthrough) return;
        _transparencyTimer.Stop();

        if (_autoTransparencyEnabled && Math.Abs(Opacity - _normalOpacity) > 0.01)
            AnimateOpacity(Opacity, _normalOpacity);
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_autoTransparencyEnabled && !_mousePassthrough)
        {
            _transparencyTimer.Stop();
            _transparencyTimer.Start();
        }
    }

    private void OnTransparencyTimerTick(object? sender, EventArgs e)
    {
        _transparencyTimer.Stop();
        if (_autoTransparencyEnabled && Math.Abs(Opacity - _idleOpacity) > 0.01)
            AnimateOpacity(Opacity, _idleOpacity);
    }

    private void AnimateOpacity(double from, double to)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        BeginAnimation(OpacityProperty, animation);
    }

    public void ToggleAutoTransparency()
    {
        _autoTransparencyEnabled = !_autoTransparencyEnabled;
        _settings.Appearance.AutoTransparency = _autoTransparencyEnabled;
        _settingsService.Save(_settings);

        if (_autoTransparencyEnabled && !IsMouseOver)
            _transparencyTimer.Start();
    }

    public void ToggleTopmost()
    {
        Topmost = !Topmost;
        _settings.Window.Topmost = Topmost;
        _settingsService.Save(_settings);
    }

    #endregion

    #region Window Drag

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Don't drag if clicking buttons/controls
        if (e.OriginalSource is Button)
            return;
        if (e.OriginalSource is TextBlock tb && ReferenceEquals(tb.Cursor, System.Windows.Input.Cursors.Hand))
            return;

        _isDragging = true;
        _dragStartPoint = e.GetPosition(this);
        CaptureMouse();
    }

    private void SettingsGear_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        OpenSettings();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var delta = e.GetPosition(this) - _dragStartPoint;
            Left += delta.X;
            Top += delta.Y;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            SaveWindowPosition();
        }
    }

    private void SaveWindowPosition()
    {
        _settings.Window.Left = Left;
        _settings.Window.Top = Top;
        _settings.Window.Width = Width;
        _settings.Window.Height = Height;
        _settingsService.Save(_settings);
    }

    #endregion

    #region Settings Window

    private void OpenSettings()
    {
        // Prevent auto-transparency while settings is open
        _transparencyTimer.Stop();

        var settingsWindow = new SettingsWindow(_settings, _settingsService);
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();

        if (settingsWindow.Saved)
        {
            // Reload all settings
            LoadSettings();
            ReloadMetrics();
        }
    }

    #endregion

    #region Mouse Passthrough

    public void ToggleMousePassthrough()
    {
        _mousePassthrough = !_mousePassthrough;

        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (_mousePassthrough)
        {
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);
            Opacity = _idleOpacity > 0 ? _idleOpacity : 0.3;
        }
        else
        {
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
            Opacity = _normalOpacity;
        }
    }

    public bool IsMousePassthroughEnabled() => _mousePassthrough;

    #endregion

    #region Multi-Monitor / DPI

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        // Clamp window to visible screen area
        var screen = System.Windows.Forms.Screen.FromRectangle(
            new System.Drawing.Rectangle((int)Left, (int)Top, (int)Width, (int)Height));

        if (screen is null) return;

        var bounds = screen.WorkingArea;
        var newLeft = Math.Clamp(Left, bounds.Left, bounds.Right - Width);
        var newTop = Math.Clamp(Top, bounds.Top, bounds.Bottom - Height);

        if (Math.Abs(newLeft - Left) > 0.1 || Math.Abs(newTop - Top) > 0.1)
        {
            Left = newLeft;
            Top = newTop;
        }
    }

    #endregion

    #region Window Lifecycle

    public void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void HideWindow() => Hide();

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_settings.Behavior.CloseToTray && e is not null)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void Cleanup()
    {
        _cts.Cancel();
        _transparencyTimer.Stop();

        foreach (var provider in _providers)
            if (provider is IDisposable d) d.Dispose();

        _refreshTimer?.Dispose();
        _cts.Dispose();
    }

    public void ForceClose()
    {
        _settings.Behavior.CloseToTray = false;
        Close();
    }

    #endregion
}
