# SysMonitor

Windows 轻量级常驻性能监视器，以桌面悬浮窗形式实时展示系统性能指标。

## 功能

- **桌面悬浮窗** — 无边框置顶窗口，可拖动，位置自动记忆
- **实时性能指标** — CPU 占用率、内存用量，每秒刷新
- **自动透明化** — 鼠标离开后自动降低透明度，移入恢复，动画平滑过渡
- **系统托盘驻留** — 关闭窗口隐藏至托盘，右键菜单快捷操作
- **单实例运行** — 防止重复启动
- **配置持久化** — 窗口位置、外观偏好自动保存到 JSON 文件

## 截图

```
┌──────────────────────────┐
│ SysMonitor               │
│                          │
│  CPU   18.2%             │
│  MEM   11.2 / 32GB  35%  │
└──────────────────────────┘
```

## 系统要求

- Windows 10 / 11
- .NET 10 Runtime（或 SDK）

## 快速开始

```bash
# 运行
cd src/PerfMonitor.App
dotnet run

# 编译
dotnet build -c Release
```

编译产物位于 `src/PerfMonitor.App/bin/Release/net10.0-windows/`。

## 使用说明

| 操作 | 效果 |
|---|---|
| 拖动窗口 | 左键按住空白区域拖动，松手自动保存位置 |
| 关闭窗口 | 隐藏到系统托盘（不退出） |
| 鼠标移入 | 恢复完全不透明 |
| 鼠标移出 | 0.8 秒后渐变至半透明 |
| 托盘双击 | 显示/隐藏窗口 |
| 托盘右键 → 退出 | 彻底退出程序 |

## 项目结构

```
src/PerfMonitor.App/
├── App.xaml / App.xaml.cs          — 应用入口，单实例检测，生命周期管理
├── MainWindow.xaml / MainWindow.xaml.cs — 悬浮窗 UI 与交互逻辑
├── GlobalUsings.cs                 — WPF/WinForms 类型消歧
├── Models/
│   ├── AppSettings.cs              — 配置模型
│   └── MetricSnapshot.cs           — 指标快照模型
├── Metrics/
│   ├── IMetricProvider.cs          — 指标采集接口
│   ├── CpuMetricProvider.cs        — CPU 使用率
│   └── MemoryMetricProvider.cs     — 内存用量
├── Services/
│   ├── SettingsService.cs          — 配置读写
│   └── TrayService.cs              — 系统托盘图标与菜单
├── ViewModels/                     — 视图模型（后续扩展）
├── Views/                          — 视图（后续扩展）
├── Utils/                          — 工具类
└── Resources/                      — 图标与主题资源
```

## 配置

配置文件路径：`%AppData%/WindowsPerfMonitor/settings.json`

```json
{
  "window": {
    "left": 1200, "top": 80,
    "width": 280, "height": 180,
    "topmost": true, "showInTaskbar": false
  },
  "appearance": {
    "theme": "dark",
    "normalOpacity": 0.95,
    "idleOpacity": 0.35,
    "autoTransparency": true,
    "fontSize": 13, "compactMode": false
  },
  "metrics": {
    "refreshIntervalMs": 1000,
    "showCpu": true, "showMemory": true,
    "showDisk": false, "showNetwork": false,
    "showGpu": false, "showTemperature": false
  },
  "behavior": {
    "startWithWindows": false,
    "minimizeToTray": true,
    "closeToTray": true,
    "singleInstance": true
  }
}
```

配置文件损坏时会自动备份并恢复默认值。

## 开发

基于 .NET 10 + WPF，使用 `System.Diagnostics.PerformanceCounter` 采集性能数据。

```bash
# 还原依赖
dotnet restore

# 编译运行
dotnet run

# 发布（单文件）
dotnet publish -c Release -r win-x64 --self-contained false
```

### Milestone

- [x] **M1** — 无边框置顶窗口、CPU/内存显示、拖动、自动透明化、托盘、配置保存
- [ ] M2 — 磁盘/网络指标、设置窗口、开机自启动、主题配置
- [ ] M3 — GPU/温度指标、迷你图表、鼠标穿透、多显示器优化
- [ ] M4 — 安装包、崩溃日志、图标资源完善

## 技术栈

- C# / .NET 10
- WPF（Windows Presentation Foundation）
- System.Diagnostics.PerformanceCounter
- System.Windows.Forms.NotifyIcon（托盘）

## 许可

MIT
