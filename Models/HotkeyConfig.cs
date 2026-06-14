using FormsKeys = System.Windows.Forms.Keys;

namespace ChrPin.Models;

public sealed class HotkeyConfig
{
    public bool Ctrl { get; set; }
    public bool Alt { get; set; } = true;
    public bool Shift { get; set; }
    public bool Win { get; set; }
    public FormsKeys Key { get; set; } = FormsKeys.P;

    public bool HasModifier => Ctrl || Alt || Shift || Win;

    public override string ToString()
    {
        var parts = new List<string>();
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Win) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join(" + ", parts);
    }

    public HotkeyConfig Clone()
    {
        return new HotkeyConfig
        {
            Ctrl = Ctrl,
            Alt = Alt,
            Shift = Shift,
            Win = Win,
            Key = Key
        };
    }

    public bool IsLegacyDefault()
    {
        return Ctrl && Alt && !Shift && !Win && Key == FormsKeys.P;
    }
}
