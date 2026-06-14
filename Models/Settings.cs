namespace ChrPin.Models;

public sealed class Settings
{
    public HotkeyConfig Hotkey { get; set; } = new();
    public bool StartWithWindows { get; set; }
    public bool ShowToast { get; set; } = true;
    public bool MinimizeToTrayOnLaunch { get; set; } = true;

    public Settings Clone()
    {
        return new Settings
        {
            Hotkey = Hotkey.Clone(),
            StartWithWindows = StartWithWindows,
            ShowToast = ShowToast,
            MinimizeToTrayOnLaunch = MinimizeToTrayOnLaunch
        };
    }
}
