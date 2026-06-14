using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ChrPin.Models;
using ChrPin.Services;
using Forms = System.Windows.Forms;

namespace ChrPin;

public partial class App : System.Windows.Application
{
    private SettingsService? _settingsService;
    private WindowPinService? _windowPinService;
    private HotkeyService? _hotkeyService;
    private Forms.NotifyIcon? _notifyIcon;
    private MainWindow? _settingsWindow;
    private Window? _messageWindow;
    private DispatcherTimer? _activeWindowTimer;
    private Settings _settings = new();
    private PinnedWindowInfo? _lastEligibleWindow;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();
        _settings = _settingsService.Load();

        _windowPinService = new WindowPinService(Process.GetCurrentProcess().Id);
        _windowPinService.OperationFailed += (_, message) => ShowTip("ChrPin", message, Forms.ToolTipIcon.Warning);

        _settingsWindow = new MainWindow(_settings);
        _settingsWindow.SettingsSaved += SettingsWindow_SettingsSaved;

        _messageWindow = CreateMessageWindow();
        _messageWindow.Show();
        _messageWindow.Hide();

        var handle = new WindowInteropHelper(_messageWindow).Handle;
        _hotkeyService = new HotkeyService(handle);
        _hotkeyService.HotkeyPressed += (_, _) => ToggleActiveWindow();

        _notifyIcon = CreateNotifyIcon();

        if (!_hotkeyService.Register(_settings.Hotkey))
        {
            ShowTip("快捷键被占用", $"无法注册 {_settings.Hotkey}，请在设置里更换。", Forms.ToolTipIcon.Warning);
        }

        StartupService.SetEnabled(_settings.StartWithWindows);
        StartActiveWindowTracking();

        if (!_settings.MinimizeToTrayOnLaunch)
        {
            ShowSettingsWindow();
        }
        else
        {
            ShowTip("ChrPin 已启动", $"按 {_settings.Hotkey} 置顶或取消置顶当前窗口。", Forms.ToolTipIcon.Info);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _activeWindowTimer?.Stop();
        _hotkeyService?.Dispose();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }

    private Window CreateMessageWindow()
    {
        return new Window
        {
            Width = 0,
            Height = 0,
            ShowInTaskbar = false,
            ShowActivated = false,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            Opacity = 0
        };
    }

    private Forms.NotifyIcon CreateNotifyIcon()
    {
        var notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "ChrPin",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };

        notifyIcon.DoubleClick += (_, _) => ShowSettingsWindow();
        notifyIcon.ContextMenuStrip.Opening += (_, _) => RebuildTrayMenu(notifyIcon.ContextMenuStrip);
        RebuildTrayMenu(notifyIcon.ContextMenuStrip);
        return notifyIcon;
    }

    private void RebuildTrayMenu(Forms.ContextMenuStrip menu)
    {
        menu.Items.Clear();

        var target = _lastEligibleWindow ?? _windowPinService?.GetActiveEligibleWindow();
        var toggleText = target is not null && _windowPinService?.IsPinned(target.Handle) == true
            ? "取消置顶当前窗口"
            : "置顶当前窗口";
        var toggleItem = new Forms.ToolStripMenuItem(toggleText);
        toggleItem.Enabled = target is not null;
        toggleItem.Click += (_, _) => ToggleWindow(target);
        menu.Items.Add(toggleItem);

        var pinnedMenu = new Forms.ToolStripMenuItem("已置顶窗口");
        var pinnedWindows = _windowPinService?.GetPinnedWindows() ?? [];
        if (pinnedWindows.Count == 0)
        {
            pinnedMenu.DropDownItems.Add(new Forms.ToolStripMenuItem("暂无") { Enabled = false });
        }
        else
        {
            foreach (var window in pinnedWindows)
            {
                var item = new Forms.ToolStripMenuItem(TrimTitle(window.Title));
                item.Click += (_, _) => UnpinWindow(window);
                pinnedMenu.DropDownItems.Add(item);
            }
        }

        menu.Items.Add(pinnedMenu);
        menu.Items.Add(new Forms.ToolStripSeparator());

        var startupItem = new Forms.ToolStripMenuItem("开机启动") { Checked = _settings.StartWithWindows };
        startupItem.Click += (_, _) =>
        {
            _settings.StartWithWindows = !_settings.StartWithWindows;
            SaveSettings(_settings);
            StartupService.SetEnabled(_settings.StartWithWindows);
        };
        menu.Items.Add(startupItem);

        var settingsItem = new Forms.ToolStripMenuItem("设置");
        settingsItem.Click += (_, _) => ShowSettingsWindow();
        menu.Items.Add(settingsItem);

        var exitItem = new Forms.ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);
    }

    private void StartActiveWindowTracking()
    {
        _activeWindowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _activeWindowTimer.Tick += (_, _) =>
        {
            var active = _windowPinService?.GetActiveEligibleWindow();
            if (active is not null)
            {
                _lastEligibleWindow = active;
            }
        };
        _activeWindowTimer.Start();
    }

    private void ToggleActiveWindow()
    {
        var result = _windowPinService?.ToggleActiveWindowTopMost();
        if (result is not null)
        {
            var state = _windowPinService?.IsPinned(result.Handle) == true ? "已置顶" : "已取消置顶";
            ShowTip("ChrPin", $"{state}: {TrimTitle(result.Title)}", Forms.ToolTipIcon.Info);
        }
    }

    private void ToggleWindow(PinnedWindowInfo? window)
    {
        if (window is null)
        {
            ShowTip("ChrPin", "没有找到可操作的窗口。", Forms.ToolTipIcon.Warning);
            return;
        }

        var result = _windowPinService?.ToggleWindowTopMost(window.Handle);
        if (result is not null)
        {
            var state = _windowPinService?.IsPinned(result.Handle) == true ? "已置顶" : "已取消置顶";
            ShowTip("ChrPin", $"{state}: {TrimTitle(result.Title)}", Forms.ToolTipIcon.Info);
        }
    }

    private void UnpinWindow(PinnedWindowInfo window)
    {
        if (_windowPinService?.UnpinWindow(window.Handle) == true)
        {
            ShowTip("ChrPin", $"已取消置顶: {TrimTitle(window.Title)}", Forms.ToolTipIcon.Info);
        }
    }

    private void SettingsWindow_SettingsSaved(object? sender, Settings newSettings)
    {
        if (_hotkeyService is not null && !_hotkeyService.Register(newSettings.Hotkey))
        {
            ShowTip("快捷键被占用", $"无法注册 {newSettings.Hotkey}，请换一个组合。", Forms.ToolTipIcon.Warning);
            return;
        }

        _settings = newSettings.Clone();
        SaveSettings(_settings);
        StartupService.SetEnabled(_settings.StartWithWindows);
        _settingsWindow?.ApplySettings(_settings);
        _settingsWindow?.ShowSaved();
        ShowTip("ChrPin", "设置已保存。", Forms.ToolTipIcon.Info);
    }

    private void SaveSettings(Settings settings)
    {
        _settingsService?.Save(settings);
        _settingsWindow?.ApplySettings(settings);
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            return;
        }

        _settingsWindow.ApplySettings(_settings);
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ShowTip(string title, string message, Forms.ToolTipIcon icon)
    {
        if (!_settings.ShowToast || _notifyIcon is null)
        {
            return;
        }

        _notifyIcon.ShowBalloonTip(2500, title, message, icon);
    }

    private void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _notifyIcon?.Dispose();
        Shutdown();
    }

    private static Icon LoadAppIcon()
    {
        var path = Environment.ProcessPath;
        return path is null ? SystemIcons.Application : Icon.ExtractAssociatedIcon(path) ?? SystemIcons.Application;
    }

    private static string TrimTitle(string title)
    {
        const int maxLength = 64;
        return title.Length <= maxLength ? title : title[..(maxLength - 1)] + "...";
    }
}
