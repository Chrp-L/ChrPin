using System.Windows;
using ChrPin.Models;
using FormsKeys = System.Windows.Forms.Keys;

namespace ChrPin;

public partial class MainWindow : Window
{
    private Settings _settings;

    public event EventHandler<Settings>? SettingsSaved;

    public MainWindow(Settings settings)
    {
        InitializeComponent();
        _settings = settings.Clone();
        ApplySettings(_settings);
    }

    public void ApplySettings(Settings settings)
    {
        _settings = settings.Clone();
        CtrlCheckBox.IsChecked = _settings.Hotkey.Ctrl;
        AltCheckBox.IsChecked = _settings.Hotkey.Alt;
        ShiftCheckBox.IsChecked = _settings.Hotkey.Shift;
        WinCheckBox.IsChecked = _settings.Hotkey.Win;
        KeyTextBox.Text = _settings.Hotkey.Key.ToString();
        StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
        ShowToastCheckBox.IsChecked = _settings.ShowToast;
        MinimizeToTrayCheckBox.IsChecked = _settings.MinimizeToTrayOnLaunch;
        StatusTextBlock.Text = string.Empty;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildSettings(out var settings))
        {
            return;
        }

        SettingsSaved?.Invoke(this, settings);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySettings(_settings);
        Hide();
    }

    private bool TryBuildSettings(out Settings settings)
    {
        settings = _settings.Clone();

        var keyText = KeyTextBox.Text.Trim();
        if (keyText.Length == 1)
        {
            keyText = keyText.ToUpperInvariant();
        }

        if (!Enum.TryParse<FormsKeys>(keyText, ignoreCase: true, out var key) ||
            key is FormsKeys.None or FormsKeys.ControlKey or FormsKeys.Menu or FormsKeys.ShiftKey or FormsKeys.LWin or FormsKeys.RWin)
        {
            StatusTextBlock.Text = "请输入有效按键，例如 P、F8、Space。";
            return false;
        }

        var hotkey = new HotkeyConfig
        {
            Ctrl = CtrlCheckBox.IsChecked == true,
            Alt = AltCheckBox.IsChecked == true,
            Shift = ShiftCheckBox.IsChecked == true,
            Win = WinCheckBox.IsChecked == true,
            Key = key
        };

        if (!hotkey.HasModifier)
        {
            StatusTextBlock.Text = "请至少选择一个组合键修饰键。";
            return false;
        }

        settings.Hotkey = hotkey;
        settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
        settings.ShowToast = ShowToastCheckBox.IsChecked == true;
        settings.MinimizeToTrayOnLaunch = MinimizeToTrayCheckBox.IsChecked == true;
        return true;
    }

    public void ShowSaved()
    {
        StatusTextBlock.Text = "已保存。";
    }
}
