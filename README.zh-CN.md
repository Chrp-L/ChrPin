# ChrPin

中文 | [English](README.en.md)

ChrPin 是一个轻量的 Windows 托盘工具，可以把普通窗口切换为始终置顶。

当你想一边工作一边固定播放器、聊天窗口、参考页面、笔记、终端或工具面板时，它会很方便。

## 功能

- 用全局快捷键置顶或取消置顶当前窗口。
- 在系统托盘菜单中管理已置顶窗口。
- 从已置顶窗口列表中单独取消某个窗口的置顶。
- 在设置窗口中修改快捷键。
- 可选开机启动。
- 可选在操作后显示托盘提示。
- 默认启动后最小化到托盘。

## 下载

从 Releases 页面下载最新版本：

https://github.com/Chrp-L/ChrPin/releases

如果下载的是 Windows x64 自包含压缩包，解压后运行 `ChrPin.exe` 即可。

## 使用

ChrPin 启动后会在系统托盘中运行。默认快捷键是：

```text
Alt + P
```

让目标窗口处于当前活动状态后，按快捷键即可置顶它。再次按同一个快捷键即可取消置顶。

你也可以右键点击托盘图标来：

- 置顶或取消置顶当前窗口。
- 查看当前已置顶窗口。
- 开启或关闭开机启动。
- 打开设置。
- 退出 ChrPin。

双击托盘图标可以打开设置窗口。

## 设置

设置窗口可以配置：

- 快捷键修饰键：`Ctrl`、`Alt`、`Shift`、`Win`
- 快捷键按键：例如 `P`、`F8` 或 `Space`
- 开机启动
- 托盘提示
- 启动后最小化到托盘

设置会保存在当前 Windows 用户的配置目录中。

## 开发

从源码运行：

```powershell
dotnet run
```

创建 Windows x64 自包含发布版：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

发布后的可执行文件位于：

```text
bin\Release\net10.0-windows\win-x64\publish\ChrPin.exe
```

## 注意

- 有些以管理员权限运行的窗口需要用管理员身份运行 ChrPin 才能操作。
- 桌面和任务栏窗口会被自动忽略。
- 已置顶窗口不会在重启后自动恢复。
