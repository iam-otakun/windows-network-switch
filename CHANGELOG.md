# Changelog / 更新日志

All notable changes to Network Switch for Windows.
本项目的重要变更都记在这里。

## v1.1.1 — 2026-11-22

- **Application icon / 应用图标**: the exe now embeds an icon — the same design as the tray icon (blue disc, two opposing white arrows) — so it no longer shows the generic blank file icon in Explorer. The icon ships in the repo as `icon.ico.b64` (base64 text, so the snapshot stays text-only); `build.cmd` decodes it and passes `/win32icon` to csc. No functional changes. / exe 嵌入了应用图标，与托盘图标同一设计（蓝圆底、双向白箭头），在资源管理器里不再是白板默认图标。图标以 `icon.ico.b64`（base64 文本）形式存放在仓库里，`build.cmd` 构建时解码并以 `/win32icon` 传给 csc。无功能变更。

## v1.1.0 — 2026-11-22

- **About dialog / 关于对话框**: shows the version (read from the assembly, so it can never drift), `Idea: Otakun`, `Coding: Dash`, and a clickable link to this repository. / 显示版本号（从程序本体读取，永不写错）、`Idea: Otakun`、`Coding: Dash` 与可点击的 GitHub 仓库链接。
- **Settings dialog / 设置对话框**: Start at logon (HKCU `Run` key, applied by the app itself so the registry always matches the setting) and automatic update checks. / 开机自动启动（写 HKCU `Run` 键，由程序自己维护，注册表与设置始终一致）与自动检查更新。
- **Update checks / 检查更新**: automatic check runs once a day against GitHub Releases and is notify-only (balloon + menu item); **Check for Updates…** in Settings checks immediately. An update downloads and self-replaces only after you confirm — never silently, because this app changes network settings with elevation. / 自动检查每天一次，只提醒（气泡 + 菜单项）；设置里的**检查更新…**立即查。更新必须点确认后才下载并自替换——不静默安装，因为本程序会提权修改网络设置。
- **Detection hysteresis / 状态检测抗抖动**: field logs showed the Wi-Fi stack occasionally returning an empty route/interface reading for a few seconds on an otherwise healthy link, which flashed "Unknown". A momentary miss now keeps the last good state for up to 60 seconds. / 实测日志显示无线协议栈偶尔会在链路健康时空读几秒，曾导致「未知」闪烁。现在短暂读空会沿用 60 秒内的上次好状态。
- Diagnostic logging is now opt-in via `NETSWITCH_DEBUG=1` (was always-on in the field builds). / 诊断日志改为设 `NETSWITCH_DEBUG=1` 才写（测试版曾一直写）。

## v1.0.0 — 2026-11-22 (field build / 实测版，未公开发布)

- First Windows build, a tray counterpart of the macOS SwiftBar plugin v2.3 with the same architecture and the same `~/.network-switch.conf` format: Profiles with Direct/Overseas gateway+DNS pairs, automatic adapter/IP detection (the adapter carrying the default route), per-switch UAC elevation with cancel-vs-error reporting, Add/Edit/Delete profiles from the menu, Restore Automatic (DHCP), tray tooltip as `Profile-Exit`, UI language follows the system. / Windows 首版：macOS SwiftBar 插件 v2.3 的托盘对应版，架构与 `~/.network-switch.conf` 格式完全一致——Profile 各存直连/出海两组网关与 DNS、网卡（认默认路由）与本机 IP 自动检测、切换时 UAC 提权且取消与失败分开提示、菜单内添加/编辑/删除 Profile、恢复自动（DHCP）、托盘提示「Profile-出口」、界面语言跟随系统。
- Field-verified on a ThinkPad T480 at home: Direct ↔ Overseas round trip confirmed in the logs (gateway and DNS landed on `.1`, then back on `.2`). / 已在 ThinkPad T480 上实测验收：直连 ↔ 出海往返切换经日志核实（网关与 DNS 落到 `.1`，再回到 `.2`）。
