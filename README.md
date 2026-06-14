# ChrPin

ChrPin is a small Windows tray app for pinning any normal window above other windows.

It is useful when you want to keep a player, chat window, reference page, note, terminal, or tool panel visible while working in another app.

## Features

- Toggle the active window as always-on-top with a global hotkey.
- Manage pinned windows from the system tray menu.
- Unpin a specific window from the pinned-window list.
- Change the hotkey in the settings window.
- Optional startup with Windows.
- Optional tray notifications after pin and unpin actions.
- Starts minimized to the tray by default.

## Download

Download the latest release from:

https://github.com/Chrp-L/ChrPin/releases

For the self-contained Windows x64 package, unzip the file and run `ChrPin.exe`.

## Usage

ChrPin starts in the system tray. The default hotkey is:

```text
Alt + P
```

Press the hotkey while another window is active to pin it. Press the same hotkey again to unpin it.

You can also right-click the tray icon to:

- Pin or unpin the current window.
- View currently pinned windows.
- Turn startup with Windows on or off.
- Open settings.
- Exit ChrPin.

Double-click the tray icon to open settings.

## Settings

The settings window lets you configure:

- Hotkey modifiers: `Ctrl`, `Alt`, `Shift`, `Win`
- Hotkey key: for example `P`, `F8`, or `Space`
- Startup with Windows
- Toast notifications
- Start minimized to tray

Settings are saved under your Windows user profile.

## Development

Run from source:

```powershell
dotnet run
```

Create a self-contained Windows x64 release build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The published executable is created under:

```text
bin\Release\net10.0-windows\win-x64\publish\ChrPin.exe
```

## Notes

- Some elevated administrator windows require running ChrPin as administrator.
- Desktop and taskbar windows are intentionally ignored.
- Pinned windows are not restored after reboot.
