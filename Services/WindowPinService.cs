using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using ChrPin.Models;

namespace ChrPin.Services;

public sealed class WindowPinService
{
    private readonly Dictionary<IntPtr, PinnedWindowInfo> _pinnedWindows = [];
    private readonly int _currentProcessId;

    public WindowPinService(int currentProcessId)
    {
        _currentProcessId = currentProcessId;
    }

    public event EventHandler<string>? OperationFailed;

    public PinnedWindowInfo? ToggleActiveWindowTopMost()
    {
        var hwnd = ResolveTargetWindow(NativeMethods.GetForegroundWindow());
        return ToggleWindowTopMost(hwnd);
    }

    public PinnedWindowInfo? ToggleWindowTopMost(IntPtr hwnd)
    {
        hwnd = ResolveTargetWindow(hwnd);
        if (!IsEligibleWindow(hwnd, out var reason))
        {
            OperationFailed?.Invoke(this, reason);
            return null;
        }

        if (_pinnedWindows.ContainsKey(hwnd) || NativeMethods.IsTopMost(hwnd))
        {
            return UnpinWindow(hwnd) ? new PinnedWindowInfo(hwnd, GetWindowTitle(hwnd)) : null;
        }

        return PinWindow(hwnd) ? new PinnedWindowInfo(hwnd, GetWindowTitle(hwnd)) : null;
    }

    public bool PinWindow(IntPtr hwnd)
    {
        hwnd = ResolveTargetWindow(hwnd);
        if (!IsEligibleWindow(hwnd, out var reason))
        {
            OperationFailed?.Invoke(this, reason);
            return false;
        }

        if (!SetTopMost(hwnd, NativeMethods.HWND_TOPMOST))
        {
            return false;
        }

        _pinnedWindows[hwnd] = new PinnedWindowInfo(hwnd, GetWindowTitle(hwnd));
        return true;
    }

    public bool UnpinWindow(IntPtr hwnd)
    {
        hwnd = ResolveTargetWindow(hwnd);
        if (!NativeMethods.IsWindow(hwnd))
        {
            _pinnedWindows.Remove(hwnd);
            return true;
        }

        if (!SetTopMost(hwnd, NativeMethods.HWND_NOTOPMOST))
        {
            return false;
        }

        _pinnedWindows.Remove(hwnd);
        return true;
    }

    public IReadOnlyList<PinnedWindowInfo> GetPinnedWindows()
    {
        foreach (var item in _pinnedWindows.Keys.ToArray())
        {
            if (!NativeMethods.IsWindow(item) || !NativeMethods.IsWindowVisible(item) || !NativeMethods.IsTopMost(item))
            {
                _pinnedWindows.Remove(item);
            }
        }

        return _pinnedWindows.Values
            .Select(info => info with { Title = GetWindowTitle(info.Handle) })
            .OrderBy(info => info.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public PinnedWindowInfo? GetActiveEligibleWindow()
    {
        var hwnd = ResolveTargetWindow(NativeMethods.GetForegroundWindow());
        return IsEligibleWindow(hwnd, out _) ? new PinnedWindowInfo(hwnd, GetWindowTitle(hwnd)) : null;
    }

    public bool IsPinned(IntPtr hwnd)
    {
        hwnd = ResolveTargetWindow(hwnd);
        return _pinnedWindows.ContainsKey(hwnd) || (NativeMethods.IsWindow(hwnd) && NativeMethods.IsTopMost(hwnd));
    }

    private bool SetTopMost(IntPtr hwnd, IntPtr insertAfter)
    {
        var flags = NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW;
        if (NativeMethods.SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0, flags))
        {
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        var message = error == 5
            ? "需要以管理员身份运行 ChrPin 才能操作这个窗口。"
            : $"无法操作这个窗口：{new Win32Exception(error).Message}";
        OperationFailed?.Invoke(this, message);
        return false;
    }

    private IntPtr ResolveTargetWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return hwnd;
        }

        var candidates = new[]
        {
            NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT),
            NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOTOWNER),
            hwnd
        }
        .Where(candidate => candidate != IntPtr.Zero)
        .Distinct()
        .ToArray();

        foreach (var candidate in candidates)
        {
            if (IsEligibleWindow(candidate, out _))
            {
                return candidate;
            }
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        var processWindow = FindBestTopLevelWindow(processId);
        return processWindow == IntPtr.Zero ? hwnd : processWindow;
    }

    private IntPtr FindBestTopLevelWindow(uint processId)
    {
        var bestWindow = IntPtr.Zero;

        NativeMethods.EnumWindows((candidate, lParam) =>
        {
            NativeMethods.GetWindowThreadProcessId(candidate, out var candidateProcessId);
            if (candidateProcessId != processId || !IsEligibleWindow(candidate, out _))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(GetWindowTitle(candidate)))
            {
                bestWindow = candidate;
                return false;
            }

            bestWindow = candidate;
            return true;
        }, IntPtr.Zero);

        return bestWindow;
    }

    private bool IsEligibleWindow(IntPtr hwnd, out string reason)
    {
        reason = string.Empty;
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            reason = "没有找到可操作的活动窗口。";
            return false;
        }

        if (!NativeMethods.IsWindowVisible(hwnd))
        {
            reason = "当前窗口不可见，不能置顶。";
            return false;
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
        if (root != IntPtr.Zero && root != hwnd)
        {
            reason = "当前焦点在窗口内部控件上，正在尝试定位它的主窗口。";
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == _currentProcessId)
        {
            reason = "ChrPin 自己的窗口不需要置顶。";
            return false;
        }

        var className = GetClassName(hwnd);
        if (className is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW" or "Button")
        {
            reason = "桌面和任务栏不能置顶。";
            return false;
        }

        return true;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var builder = new StringBuilder(512);
        NativeMethods.GetWindowText(hwnd, builder, builder.Capacity);
        var title = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(title) ? $"窗口 0x{hwnd.ToInt64():X}" : title;
    }

    private static string GetClassName(IntPtr hwnd)
    {
        var builder = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }
}
