using System.Windows.Interop;
using ChrPin.Models;

namespace ChrPin.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x5019;
    private readonly IntPtr _windowHandle;
    private readonly HwndSource _source;
    private HotkeyConfig? _registeredConfig;

    public HotkeyService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle) ?? throw new InvalidOperationException("Hotkey message window is unavailable.");
        _source.AddHook(WndProc);
    }

    public event EventHandler? HotkeyPressed;

    public bool Register(HotkeyConfig config)
    {
        var previous = _registeredConfig?.Clone();
        Unregister();

        if (!NativeMethods.RegisterHotKey(_windowHandle, HotkeyId, ToModifiers(config), (uint)config.Key))
        {
            if (previous is not null)
            {
                NativeMethods.RegisterHotKey(_windowHandle, HotkeyId, ToModifiers(previous), (uint)previous.Key);
                _registeredConfig = previous;
            }

            return false;
        }

        _registeredConfig = config.Clone();
        return true;
    }

    public void Unregister()
    {
        if (_registeredConfig is null)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_windowHandle, HotkeyId);
        _registeredConfig = null;
    }

    private static uint ToModifiers(HotkeyConfig config)
    {
        var modifiers = NativeMethods.MOD_NOREPEAT;
        if (config.Ctrl) modifiers |= NativeMethods.MOD_CONTROL;
        if (config.Alt) modifiers |= NativeMethods.MOD_ALT;
        if (config.Shift) modifiers |= NativeMethods.MOD_SHIFT;
        if (config.Win) modifiers |= NativeMethods.MOD_WIN;
        return modifiers;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        GC.SuppressFinalize(this);
    }
}
