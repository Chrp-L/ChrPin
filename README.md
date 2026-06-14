# ChrPin

ChrPin is a lightweight Windows 11 tray utility for toggling the active window as always-on-top.

## Run

```powershell
dotnet run
```

The app starts in the system tray by default. Press `Alt + P` to pin or unpin the active window.

## Publish

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

The published executable is created under:

```text
bin\Release\net10.0-windows\win-x64\publish\ChrPin.exe
```

## Notes

- Right-click the tray icon to open settings, toggle startup, view pinned windows, or exit.
- Some elevated administrator windows require running ChrPin as administrator.
- Pinned windows are intentionally not restored after reboot.
