# Changelog / 更新日志

All notable changes to Network Switch for Windows.
本项目的重要变更都记在这里。

## v1.2.5 — 2026-11-24

- **Menu layout aligned with BingWallpaper / 菜单分组与 BingWallpaper 对齐**: the tray menu's last group is now always **Settings… / Check for Updates... / About… / Quit**, with the functional items in the groups above it — **Add Profile...** and **Restore Automatic (DHCP)** form their own group, followed by a separator. **Check for Updates...** is new as a menu item (it previously lived only inside the Settings dialog) and runs the same manual check; the stray separator that used to sit between About and Quit is gone. Same regrouping in the not-configured menu. / 托盘菜单的最后一组从此固定为 **设置… / 检查更新… / 关于… / 退出**，功能项都归到上面的组：**添加 Profile…** 与 **恢复自动（DHCP）** 自成一组，其后加分割线。**检查更新…** 新增为菜单项（此前只藏在设置对话框里），跑的是同一个手动检查；About 与 Quit 之间那条多余的分割线去掉。未配置状态下的菜单同样重新分组。

## v1.2.4 — 2026-11-24

- **Unified About / 统一 About**: the About dialog's GitHub link now reads **GitHub Repository** — in v1.1.0–v1.2.3 the raw URL itself was the link text, which Fred called (rightly) crude. This dialog is the reference for the unified About rolled out to all our Windows and macOS programs in this round: name, version, Idea: Otakun, Coding: Dash, and a link labelled "GitHub Repository". No functional changes. / About 对话框里的 GitHub 链接现在显示为 **GitHub Repository**——v1.1.0–v1.2.3 里链接文字就是那串原始 URL，Fred 说它粗糙，说得对。这个对话框是本轮统一 About 的样板，推广到我们全部 Windows 与 macOS 程序：程序名、版本、Idea: Otakun、Coding: Dash、一条写着「GitHub Repository」的链接。无功能变化。

## v1.2.3 — 2026-11-23

- **Mutual exclusion, both directions / 互斥补全反方向**: the Proxy item is now also greyed out while the measured gateway matches a Profile exit — in profile mode the configured proxy address (e.g. the phone hotspot's `172.20.10.1`) belongs to a foreign network and is not even reachable, so switching the proxy on there cannot work. The way back is **Restore Automatic (DHCP)** or moving to a network that matches no Profile. The Proxy item stays clickable while the proxy is on (it is how you switch it off) and on foreign networks. Net effect: gateway mode and proxy mode are two mutually exclusive ways out, and every transition passes through the neutral state. / 当实测网关对上某个 Profile 出口时（网关模式），Proxy 菜单项同样变灰——那个模式下设定里的代理地址（如手机热点的 `172.20.10.1`）属于另一个网络，在这里根本够不着，开了也没用。回中立态的路是**恢复自动（DHCP）**，或换到对不上任何 Profile 的网络。Proxy 开着时它本身永远可点（要靠它关），在陌生网络上也可点。最终效果：网关模式与代理模式是两个互斥的出口，任何切换都必须经过中立态。

## v1.2.2 — 2026-11-23

- **Proxy and Profiles are mutually exclusive / Proxy 与 Profile 互斥**: while the Proxy is on, both exits (Direct / Overseas) of every Profile are greyed out and cannot be selected — proxy mode borrows another device's Clash, so the local gateway must stay exactly as the LAN or hotspot assigned it; pinning a profile gateway at the same time would contradict that. Turning the Proxy off makes the exits selectable again. Edit and Delete stay available either way; they do not touch the network. / Proxy 打开时，所有 Profile 的两个出口（直连 / 出海）都变灰、不可选——代理模式是借道另一台设备的 Clash，本机网关必须原样保留局域网或热点分给它的样子，此时再钉一个 Profile 网关就自相矛盾了。关掉 Proxy，出口立即恢复可选。编辑与删除不受影响，它们不动网络。

## v1.2.1 — 2026-11-23

- **Status line names the measured profile / 状态行按实测归属**: the menu status line and the tray tooltip now show the profile whose exit the current gateway actually matches. When the connection matches no profile at all (e.g. a phone hotspot), they show just "Unknown" with no profile name — v1.2.0's "Home-Unknown" implied a relation to Home that does not exist. / 菜单状态行与托盘提示改为显示实测网关真正对上的那个 Profile。当现在的连接跟任何 Profile 都对不上时（例如手机热点），只显示「未知」，不带 Profile 名——v1.2.0 的「Home-未知」会让人误以为跟 Home 有关系，其实没有。

## v1.2.0 — 2026-11-23

- **Proxy toggle / 代理开关**: a **Proxy** item now sits in the menu right under the Profiles. Click it and it gains a checkmark and the Windows system proxy switches on, pointing at the configured address; click again and the checkmark clears and the proxy switches off. The address lives in **Settings** as **Proxy IP** / **Proxy Port** text boxes, preset to `172.20.10.1` / `7890` — a phone hotspot sharing Clash Mi (mixed port). The checkmark reflects the real system state: it shows only when the system proxy is enabled and points at the configured address, and editing the address while the proxy is on re-points it immediately. / 菜单里两个 Profile 下面多了一个 **Proxy** 菜单项：点一下打勾，Windows 系统代理随即打开并指向设定地址；再点一下取消勾，代理关闭。地址在**设置**里以**代理 IP** / **代理端口**两个文本框填写，预设 `172.20.10.1` / `7890`——手机个人热点共享 Clash Mi（混合端口）的地址。勾选状态以系统真实状态为准：只有系统代理已启用且指向设定地址时才打勾；在代理开启时修改地址会立即改指新地址。
- **Checkmarks follow the measured gateway / 勾选改按实测网关**: Profile and exit checkmarks are now computed from the gateway the machine actually has, per profile, instead of from the saved ACTIVE name. On a phone hotspot the menu used to show "Current: Home-Unknown" while Home still carried a checkmark; now nothing is checked on a foreign network, and after **Restore Automatic (DHCP)** a checkmark appears only if the DHCP-assigned gateway actually matches a profile exit. / Profile 与出口的勾选改由本机实测网关逐个 Profile 计算，不再看保存的 ACTIVE 名字。此前用手机热点上网时菜单显示「当前：Home-未知」，Home 项却还带着勾；现在身处陌生网络时一律不打勾，**恢复自动（DHCP）**之后也只有 DHCP 分到的网关确实对上某个 Profile 出口时才打勾。

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
