using System.Drawing;
using System.Windows;
using PerfMonitor.App.Models;

namespace PerfMonitor.App.Services;

public class TrayService : IDisposable
{
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly System.Windows.Forms.ContextMenuStrip _contextMenu;
    private Window? _mainWindow;
    private bool _disposed;

    public event Action? ShowWindowRequested;
    public event Action? ExitRequested;
    public event Action? ToggleTopmostRequested;
    public event Action? ToggleAutoTransparencyRequested;
    public event Action? SettingsRequested;
    public event Action? TogglePassthroughRequested;
    public event Action? ToggleStartWithWindowsRequested;

    public TrayService(AppSettings settings, SettingsService settingsService)
    {
        _settings = settings;
        _settingsService = settingsService;

        _contextMenu = new System.Windows.Forms.ContextMenuStrip();
        BuildContextMenu();

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "PerfMonitor",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindowRequested?.Invoke();
    }

    public void SetMainWindow(Window window)
    {
        _mainWindow = window;
        _mainWindow.StateChanged += (_, _) => UpdateContextMenu();
    }

    public void UpdateTooltip(string text)
    {
        if (_disposed) return;
        _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
    }

    private void BuildContextMenu()
    {
        _contextMenu.Items.Clear();

        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Show/Hide", null,
            (_, _) => ShowWindowRequested?.Invoke()));

        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var topmostItem = new System.Windows.Forms.ToolStripMenuItem("Topmost",
            null, (_, _) => ToggleTopmostRequested?.Invoke())
        {
            Checked = _settings.Window.Topmost
        };
        _contextMenu.Items.Add(topmostItem);

        var transparencyItem = new System.Windows.Forms.ToolStripMenuItem("Auto Transparency",
            null, (_, _) => ToggleAutoTransparencyRequested?.Invoke())
        {
            Checked = _settings.Appearance.AutoTransparency
        };
        _contextMenu.Items.Add(transparencyItem);

        var passthroughItem = new System.Windows.Forms.ToolStripMenuItem("Mouse Passthrough",
            null, (_, _) => TogglePassthroughRequested?.Invoke())
        {
            Checked = false
        };
        _contextMenu.Items.Add(passthroughItem);

        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var startupItem = new System.Windows.Forms.ToolStripMenuItem("Start with Windows",
            null, (_, _) => ToggleStartWithWindowsRequested?.Invoke())
        {
            Checked = StartupService.IsAutoStartEnabled()
        };
        _contextMenu.Items.Add(startupItem);

        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Settings", null,
            (_, _) => SettingsRequested?.Invoke()));

        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Exit", null,
            (_, _) => ExitRequested?.Invoke()));
    }

    public void UpdateContextMenu()
    {
        if (_disposed || _contextMenu.Items.Count < 7) return;

        // Index 2: Topmost
        if (_contextMenu.Items[2] is System.Windows.Forms.ToolStripMenuItem topmostItem)
            topmostItem.Checked = _settings.Window.Topmost;

        // Index 3: Auto Transparency
        if (_contextMenu.Items[3] is System.Windows.Forms.ToolStripMenuItem transparencyItem)
            transparencyItem.Checked = _settings.Appearance.AutoTransparency;

        // Index 6: Start with Windows
        if (_contextMenu.Items[6] is System.Windows.Forms.ToolStripMenuItem startupItem)
            startupItem.Checked = StartupService.IsAutoStartEnabled();
    }

    public void SetPassthroughChecked(bool isChecked)
    {
        if (_disposed || _contextMenu.Items.Count < 5) return;
        if (_contextMenu.Items[4] is System.Windows.Forms.ToolStripMenuItem passthroughItem)
            passthroughItem.Checked = isChecked;
    }

    private static Icon CreateTrayIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(System.Drawing.Color.FromArgb(0, 30, 30, 30));

        using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 120, 215), 2);
        g.DrawRectangle(pen, 4, 20, 6, 8);
        g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215)), 4, 20, 6, 8);
        g.DrawRectangle(pen, 13, 12, 6, 16);
        g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0, 200, 120)), 13, 12, 6, 16);
        g.DrawRectangle(pen, 22, 4, 6, 24);
        g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(255, 180, 0)), 22, 4, 6, 24);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
