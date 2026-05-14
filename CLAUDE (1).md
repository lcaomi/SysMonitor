# CLAUDE.md

本文件用于指导 Claude Code 在本仓库中开发一个 **Windows 常驻性能监视器**。项目目标是实现一个低资源占用、可后台常驻、前台置顶、自动透明化、支持图标/托盘显示的桌面性能监控工具。

---

## 1. 项目定位

开发一个面向 Windows 用户的轻量级常驻性能监视器，类似桌面悬浮窗/小组件，用于实时展示 CPU、内存、磁盘、网络、GPU 等系统性能指标。

核心特点：

- 后台常驻运行
- 桌面前台置顶显示
- 鼠标悬停时正常显示，鼠标离开后自动透明化
- 支持系统托盘图标与右键菜单
- 支持最小化到托盘、开机自启动、配置持久化
- 资源占用低，不应明显影响被监控系统性能

---

## 2. 推荐技术栈

优先选择 Windows 原生体验较好的实现方式。

### 2.1 首选方案

- 语言：C#
- 框架：.NET 8 或更新 LTS 版本
- UI：WPF
- 系统托盘：Hardcodet.NotifyIcon.Wpf 或 WinForms NotifyIcon
- 性能数据：
  - System.Diagnostics.PerformanceCounter
  - Windows Management Instrumentation / WMI
  - PDH / Windows API，必要时封装
  - GPU 数据可优先使用 LibreHardwareMonitorLib 或 Windows Performance Counters
- 配置存储：JSON 文件
- 日志：Serilog 或 Microsoft.Extensions.Logging

### 2.2 不推荐方案

除非用户特别要求，否则不要优先使用：

- Electron：资源占用偏高，不符合轻量常驻工具定位
- 浏览器内核方案：不适合作为低占用系统监控悬浮窗
- 纯控制台程序：不满足前台置顶和托盘交互需求

---

## 3. 功能需求

### 3.1 后台常驻

应用启动后应进入常驻模式：

- 关闭主窗口时默认隐藏到系统托盘，而不是直接退出
- 托盘菜单中提供“显示/隐藏”“设置”“退出”
- 程序异常时应记录日志，避免静默崩溃
- 支持单实例运行，防止重复启动多个监视器窗口

实现要求：

- 使用 Mutex 或命名管道实现单实例检测
- 主窗口隐藏后仍保持采样定时器运行
- 退出必须通过托盘菜单或设置中的明确退出操作

---

### 3.2 前台置顶悬浮窗

监视器主窗口应作为桌面悬浮窗显示：

- 无边框窗口
- 可拖动
- Topmost 置顶
- 不在任务栏显示，或提供配置项控制是否显示
- 支持记忆上次窗口位置
- 支持多显示器环境

WPF 窗口建议属性：

```xml
WindowStyle="None"
AllowsTransparency="True"
Topmost="True"
ShowInTaskbar="False"
ResizeMode="NoResize"
Background="Transparent"
```

拖动行为：

- 左键按住窗口空白区域可拖动
- 拖动结束后保存窗口位置
- 不应在点击按钮、菜单、图表区域时误触发拖动

---

### 3.3 自动透明化

窗口需要支持自动透明化，提高日常使用时的非侵入性。

行为规则：

- 鼠标进入窗口区域：恢复正常不透明度
- 鼠标离开窗口区域：延迟一小段时间后降低透明度
- 支持配置透明度数值，例如：
  - 正常透明度：0.90–1.00
  - 空闲透明度：0.20–0.60
- 支持开关：是否启用自动透明化
- 支持动画过渡，避免突兀变化

建议实现：

- MouseEnter 恢复 Opacity
- MouseLeave 启动 DispatcherTimer 延迟透明化
- 使用 DoubleAnimation 实现平滑过渡
- 当打开右键菜单或设置弹窗时，不应自动透明化

注意：

- 不要让窗口透明到完全不可见
- 最低透明度建议限制为 0.15
- 如果用户开启“鼠标穿透”，需要单独处理命中测试逻辑

---

### 3.4 图标显示功能

这里的“图标显示”包含两层含义：系统托盘图标与性能项图标。

#### 3.4.1 系统托盘图标

托盘图标必须具备：

- 应用图标
- 鼠标悬停提示文本，例如当前 CPU / 内存占用
- 右键菜单：
  - 显示/隐藏监视器
  - 置顶开关
  - 自动透明化开关
  - 设置
  - 开机自启动
  - 退出
- 双击托盘图标：显示或隐藏窗口

托盘菜单操作应即时生效，并写入配置。

#### 3.4.2 性能项图标

前台悬浮窗中每个指标建议带图标：

- CPU：芯片图标
- 内存：内存条图标
- 磁盘：硬盘图标
- 网络：上下行箭头图标
- GPU：显卡图标
- 温度：温度计图标
- 电池：电池图标，仅笔记本显示

图标建议使用：

- Segoe MDL2 Assets
- Fluent UI System Icons
- 内置 SVG Path
- 本地矢量资源

不要依赖运行时联网加载图标。

---

### 3.5 性能指标采集

最低可用版本必须支持：

- CPU 总占用率
- 内存已用 / 总量 / 百分比
- 网络上传 / 下载速度
- 磁盘读写速度

增强版本可支持：

- 单进程排行
- GPU 占用率
- 显存占用
- CPU / GPU 温度
- 电池电量与充放电状态
- FPS 或游戏相关指标，若可行

采样策略：

- 默认 1 秒采样一次
- UI 刷新频率不应高于采样频率太多
- 采样线程不能阻塞 UI 线程
- WMI 查询应控制频率，避免高开销
- 每类指标封装为独立 Provider

建议接口：

```csharp
public interface IMetricProvider
{
    string Name { get; }
    Task<MetricSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
```

---

## 4. 非功能需求

### 4.1 低资源占用

目标：

- 空闲 CPU 占用接近 0%
- 内存占用尽量低
- 不频繁分配大对象
- 不在 UI 线程执行耗时采集

要求：

- 使用 Dispatcher 只更新最终 UI 状态
- Provider 层异步采集
- 避免每秒创建大量临时对象
- 日志写入控制级别，避免刷盘过多

---

### 4.2 稳定性

- 任何单项指标采集失败，不应导致整个程序崩溃
- 采集异常需要降级显示为 `N/A`
- 托盘图标必须在退出时释放
- 配置文件损坏时应使用默认配置并备份坏文件

---

### 4.3 隐私与安全

- 默认不上传任何性能数据
- 不包含遥测，除非用户明确开启
- 配置文件仅保存本地偏好
- 不请求管理员权限，除非某些硬件指标确实需要
- 如需管理员权限，应在 UI 中明确说明原因

---

## 5. 推荐项目结构

```text
src/
  PerfMonitor.App/
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    Views/
    ViewModels/
    Services/
      TrayService.cs
      SettingsService.cs
      StartupService.cs
      WindowBehaviorService.cs
    Metrics/
      IMetricProvider.cs
      CpuMetricProvider.cs
      MemoryMetricProvider.cs
      DiskMetricProvider.cs
      NetworkMetricProvider.cs
      GpuMetricProvider.cs
      MetricSnapshot.cs
    Models/
      AppSettings.cs
      DisplayMetric.cs
    Resources/
      Icons/
      Themes/
    Utils/
  PerfMonitor.Tests/
```

---

## 6. 配置设计

配置文件建议路径：

```text
%AppData%/WindowsPerfMonitor/settings.json
```

示例配置：

```json
{
  "window": {
    "left": 1200,
    "top": 80,
    "width": 260,
    "height": 180,
    "topmost": true,
    "showInTaskbar": false
  },
  "appearance": {
    "theme": "dark",
    "normalOpacity": 0.95,
    "idleOpacity": 0.35,
    "autoTransparency": true,
    "fontSize": 13,
    "compactMode": false
  },
  "metrics": {
    "refreshIntervalMs": 1000,
    "showCpu": true,
    "showMemory": true,
    "showDisk": true,
    "showNetwork": true,
    "showGpu": true,
    "showTemperature": false
  },
  "behavior": {
    "startWithWindows": false,
    "minimizeToTray": true,
    "closeToTray": true,
    "singleInstance": true
  }
}
```

---

## 7. UI 设计原则

整体风格：

- 小型悬浮卡片
- 深色半透明背景
- 圆角
- 轻微阴影
- 信息密度高但不拥挤
- 指标颜色直观，但避免过度刺眼

建议布局：

```text
┌────────────────────────┐
│ CPU  18%        3.8GHz │
│ MEM  11.2 / 32GB  35%  │
│ NET  ↑120K  ↓3.2M      │
│ DISK R 12M  W 1.1M     │
│ GPU  42%    VRAM 4.1G  │
└────────────────────────┘
```

UI 细节：

- 每行左侧显示图标
- 中间显示指标名称
- 右侧显示数值
- 可选迷你折线图，但默认不要过重
- 紧凑模式只显示图标和核心百分比

---

## 8. 开发里程碑

### Milestone 1：最小可用版本

- WPF 无边框置顶窗口
- CPU / 内存显示
- 可拖动
- 自动透明化
- 托盘图标与退出菜单
- 配置保存窗口位置

### Milestone 2：完整常驻体验

- 关闭到托盘
- 单实例
- 开机自启动
- 磁盘 / 网络指标
- 设置窗口
- 主题与透明度配置

### Milestone 3：增强指标与体验

- GPU 指标
- 温度指标
- 指标排序与显隐配置
- 迷你图表
- 鼠标穿透模式
- 多显示器优化

### Milestone 4：发布准备

- 安装包
- 自动更新，可选
- 崩溃日志
- README
- 图标资源完善
- 签名，可选

---

## 9. Claude Code 工作准则

在本项目中，Claude Code 应遵守以下规则：

1. 优先实现可运行的最小闭环，而不是一次性堆叠所有功能。
2. 涉及 Windows API、托盘、置顶、透明窗口时，应给出具体可运行代码。
3. 不要引入重型依赖，除非收益明显。
4. 所有配置项必须有默认值。
5. 所有后台定时器、托盘对象、CancellationTokenSource 都必须正确释放。
6. UI 线程只负责渲染，不执行耗时性能采集。
7. 每次新增指标 Provider，都要处理异常和不可用状态。
8. 修改窗口行为时，要考虑多显示器和 DPI 缩放。
9. 不要假设用户具有管理员权限。
10. 新增功能时同步更新 README 或本指导文件中的相关说明。

---

## 10. 验收标准

最小版本完成时应满足：

- 启动后显示置顶悬浮窗
- CPU 和内存数据每秒刷新
- 鼠标移入窗口恢复不透明，移出后自动变透明
- 窗口可拖动，重启后位置保留
- 点击关闭按钮后隐藏到托盘
- 托盘右键可以显示、隐藏、退出
- 程序退出后无残留托盘图标
- 空闲运行时资源占用低
- 配置文件可读写，损坏时可恢复默认配置

---

## 11. 后续可选能力

可在核心功能稳定后再考虑：

- 插件式指标 Provider
- 自定义布局编辑器
- 皮肤市场或主题导入
- 多窗口监控面板
- 进程级性能排行
- 游戏模式自动隐藏
- 鼠标穿透
- 热键显示/隐藏
- 便携版模式

---

## 12. 首次开发建议 Prompt

如果从零开始，请让 Claude Code 先执行：

```text
请基于本 CLAUDE.md 创建一个 .NET 8 WPF 项目，实现 Milestone 1：无边框置顶悬浮窗、CPU/内存实时显示、自动透明化、系统托盘图标、关闭到托盘、窗口位置配置保存。要求代码可运行，结构按本文档推荐目录组织。
```
