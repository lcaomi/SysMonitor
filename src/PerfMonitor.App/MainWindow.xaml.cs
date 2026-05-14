using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PerfMonitor.App.Metrics;
using PerfMonitor.App.Models;
using PerfMonitor.App.Services;

namespace PerfMonitor.App;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly List<IMetricProvider> _providers = [];
    private readonly PeriodicTimer _refreshTimer;
    private readonly CancellationTokenSource _cts = new();

    // Auto-transparency
    private readonly DispatcherTimer _transparencyTimer;
    private bool _autoTransparencyEnabled;
    private double _normalOpacity;
    private double _idleOpacity;

    // Window dragging
    private bool _isDragging;
    private Point _dragStartPoint;

    public MainWindow(AppSettings settings, SettingsService settingsService)
    {
        InitializeComponent();

        _settings = settings;
        _settingsService = settingsService;

        // Apply settings
        LoadSettings();

        // Wire up providers for Milestone 1: CPU + Memory
        _providers.Add(new CpuMetricProvider());
        _providers.Add(new MemoryMetricProvider());

        // Refresh timer
        var interval = TimeSpan.FromMilliseconds(_settings.Metrics.RefreshIntervalMs);
        _refreshTimer = new PeriodicTimer(interval);

        // Auto-transparency timer (fires after mouse leaves)
        _transparencyTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        _transparencyTimer.Tick += OnTransparencyTimerTick;

        // Start sampling loop
        _ = RunSamplingLoopAsync(_cts.Token);

        // Handle window closing
        Closing += OnWindowClosing;
        Closed += (_, _) => Cleanup();
    }

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

    }

    private async Task RunSamplingLoopAsync(CancellationToken ct)
    {
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
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Sampling error — show N/A on next successful cycle
            }
        }
    }

    private void UpdateUI(List<MetricSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            switch (snapshot.Name)
            {
                case "CPU":
                    CpuValue.Text = snapshot.IsAvailable ? snapshot.Value : "N/A";
                    if (!snapshot.IsAvailable)
                        CpuValue.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                    break;
                case "MEM":
                    MemValue.Text = snapshot.IsAvailable ? snapshot.Value : "N/A";
                    if (!snapshot.IsAvailable)
                        MemValue.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                    break;
            }
        }
    }

    #region Auto Transparency

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        _transparencyTimer.Stop();

        if (_autoTransparencyEnabled && Math.Abs(Opacity - _normalOpacity) > 0.01)
        {
            AnimateOpacity(Opacity, _normalOpacity);
        }
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_autoTransparencyEnabled)
        {
            _transparencyTimer.Stop();
            _transparencyTimer.Start();
        }
    }

    private void OnTransparencyTimerTick(object? sender, EventArgs e)
    {
        _transparencyTimer.Stop();

        if (_autoTransparencyEnabled && Math.Abs(Opacity - _idleOpacity) > 0.01)
        {
            AnimateOpacity(Opacity, _idleOpacity);
        }
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
        {
            _transparencyTimer.Start();
        }
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
        // Only start dragging on the window itself (border/grid), not on buttons
        if (e.OriginalSource is System.Windows.Controls.Button or System.Windows.Controls.Primitives.Thumb)
            return;

        _isDragging = true;
        _dragStartPoint = e.GetPosition(this);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPos = e.GetPosition(this);
            var delta = currentPos - _dragStartPoint;

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

    #region Window Lifecycle

    public void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void HideWindow()
    {
        Hide();
    }

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
        {
            if (provider is IDisposable d)
                d.Dispose();
        }

        _refreshTimer.Dispose();
        _cts.Dispose();
    }

    public void ForceClose()
    {
        // Called from tray exit — bypasses close-to-tray
        _settings.Behavior.CloseToTray = false;
        Close();
    }

    #endregion
}
