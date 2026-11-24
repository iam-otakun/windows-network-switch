# Network Switch for Windows / 网络切换（Windows 版）

A tiny Windows system-tray app that switches the active network connection between **two gateways on the same LAN** with one click. Locations are kept as **Profiles** — take the same laptop home and to the office, and each place has its own pair of gateways. It is the Windows counterpart of the macOS SwiftBar plugin [swiftbar-network-switch](https://github.com/iam-otakun/swiftbar-network-switch), and both editions share the **same config file format**.

一个小巧的 Windows 托盘程序：在系统托盘一键切换当前连接的**同网段双网关**。不同地点存为 **Profile**——同一台笔记本带回家、带到办公室，每个地方有自己的一对网关。它是 macOS SwiftBar 插件 [swiftbar-network-switch](https://github.com/iam-otakun/swiftbar-network-switch) 的 Windows 对应版，两个版本**共用同一份配置文件格式**。

## About this app / 关于这个应用

Built by **Otakun** (idea) together with **Dash**, his Muse assistant (coding), for a specific daily routine: the same laptop moving between home and office, where each LAN has two gateways — one direct, one via a bypass router. It is the Windows edition of our own macOS plugin, built because we needed it every day, and shared in case your network looks the same.

由 **Otakun** 出想法、他的 Muse 助手 **Dash** 写代码，为一个具体的日常而做：同一台笔记本在家与办公室之间移动，每个局域网里都住着两个网关——一个直连、一个走旁路由。它是我们自家 macOS 插件的 Windows 版，因为自己天天要用才做了出来；如果你的网络也是这个样子，希望它对你有用。

## Why / 为什么做这个

Some networks have two ways out living on the same subnet — for example a campus/office LAN where the default router (`.1`) reaches the local network and the internet directly, while a second box (`.2`, e.g. a bypass router running a proxy) routes traffic through a different exit. Windows has no quick built-in way to flip the gateway and DNS of a connection back and forth. So this app puts the switch in the system tray, organised by place:

有些网络里，同一个子网住着两个出口——比如学校/办公室局域网：默认路由器（`.1`）直达本地与互联网，另一台盒子（`.2`，例如跑代理的旁路由）则把流量送往另一个出口。Windows 没有现成的快捷方式来回切换一个连接的网关与 DNS。于是这个程序把开关放进系统托盘，并按地点组织：

- The tray tooltip and the menu status line show the current state as **Profile–Exit**, e.g. `Home-Overseas` or `Office-Direct`, with gateway, DNS, adapter and this machine's IP. The profile named is the one the measured gateway actually matches; on a network that matches no Profile, they show just **Unknown**, with no profile name claimed. / 托盘提示与菜单状态行以**「Profile 名-出口名」**显示当前状态，如 `Home-Overseas`、`Office-Direct`，并附网关、DNS、网卡与本机 IP。所显示的 Profile 是实测网关真正对上的那个；在对不上任何 Profile 的网络上，只显示**「未知」**，不挂任何 Profile 的名字。
- The menu lists the **Profiles at the top level**; each Profile opens a submenu with its two exits — **Direct** and **Overseas** — each with its own gateway and DNS. Clicking an exit applies it and makes that Profile the current one. / 菜单里 **Profile 是一级菜单**，每个 Profile 的子菜单里是它的两个出口——**直连（Direct）**与**出海（Overseas）**——各有自己的网关与 DNS。点中某个出口即切换过去，并把该 Profile 定为当前。
- **Add Profile…** in the menu creates a new Profile: a name plus a gateway and a DNS field for each exit, with IPv4 validation. Each Profile's submenu also carries **Edit** (rename included) and **Delete**. / 菜单里的**添加 Profile…**即可新建：一个名字，外加每个出口各一组网关与 DNS，带 IPv4 格式校验。每个 Profile 的子菜单里还有**编辑**（含改名）与**删除**。
- **The adapter and this machine's own IP are detected automatically**: the adapter follows the current default route, and switching keeps the IP and subnet mask the machine already has, changing only the gateway and DNS. / **网卡与本机 IP 全自动检测**：网卡认当前默认路由的那块，切换时沿用本机已有的 IP 与掩码，只改网关与 DNS。
- A **Restore Automatic (DHCP)** item puts the connection back to automatic when you want out. / 菜单里有**恢复自动（DHCP）**，想回到全自动时点它。
- A **Proxy** item under the Profiles toggles the Windows system proxy: one click turns it on (checkmark) pointing at the address set in **Settings** (**Proxy IP** / **Proxy Port**, preset `172.20.10.1` / `7890` — e.g. a phone hotspot sharing Clash Mi), the next click turns it off. The two ways out are mutually exclusive, in both directions: while the Proxy is on, every Profile's exits are greyed out — proxy mode borrows another device's Clash, so the local gateway must stay exactly as the network assigned it. And while the measured gateway matches a Profile exit, the Proxy item is greyed out — the configured proxy address belongs to another network and is not reachable from here. Moving between the two modes always passes through the neutral state: **Restore Automatic (DHCP)**, or a network that matches no Profile. / Profile 下面有一个 **Proxy** 菜单项，切换 Windows 系统代理：点一下打开（打勾）并指向**设置**里的地址（**代理 IP** / **代理端口**，预设 `172.20.10.1` / `7890`，例如手机个人热点共享的 Clash Mi），再点一下关闭。两个出口双向互斥：Proxy 开着时，所有 Profile 的出口变灰不可选——代理模式借道另一台设备的 Clash，本机网关必须原样保留网络分给它的样子；反过来，实测网关对上某个 Profile 出口时，Proxy 变灰——设定里的代理地址属于另一个网络，在这里根本够不着。两种模式之间切换，必须经过中立态：**恢复自动（DHCP）**，或换到对不上任何 Profile 的网络。
- Menu checkmarks follow the gateway the machine actually has: on a foreign network nothing is checked, never a stale checkmark from the last profile used. / 菜单勾选以本机实测网关为准：身处陌生网络时不打勾，绝不残留上一个 Profile 的勾。
- **Settings** offers **Start at logon** and an automatic daily update check (notify only). Updates are never installed silently: a found update appears as a menu item and balloon, and installs only after you confirm. A manual **Check for Updates…** lives in the same dialog. / **设置**里有**开机自动启动**与每天一次的自动检查更新（仅提醒）。更新永不静默安装：发现新版会以菜单项和气泡提示，点确认后才下载替换。手动**检查更新…**也在同一个对话框里。
- The **About** dialog shows the version, credits and a link to this repository. / **关于**对话框里有版本号、作者信息与本仓库链接。
- The UI language follows the system (Chinese systems get Chinese, everything else English); `NETSWITCH_LANG` overrides. / 界面语言跟随系统（中文系统用中文，其它用英文）；环境变量 `NETSWITCH_LANG` 可覆盖。
- Settings are stored in `%USERPROFILE%\.network-switch.conf` — the same format as the macOS edition, so a config can be carried across. / 设置保存在 `%USERPROFILE%\.network-switch.conf`，与 macOS 版格式相同，配置可以两边搬。

Switching asks for elevation (UAC) for just the apply step, because changing a gateway/DNS requires privileges; cancelling the UAC prompt is reported separately from a real failure, and failures show the system's own error text. The app itself runs unelevated.

切换时仅在应用那一步弹 UAC 提权——修改网关/DNS 本就需要权限；在 UAC 点取消与真正失败会分开提示，失败时显示系统原文报错。程序本体以普通权限运行。

## Install / 安装

No installer — a single exe:

无需安装，单个 exe：

1. Download `NetworkSwitch.exe` from the [latest release](https://github.com/iam-otakun/windows-network-switch/releases/latest), or build it yourself (see below). / 从[最新 Release](https://github.com/iam-otakun/windows-network-switch/releases/latest) 下载 `NetworkSwitch.exe`，或按下面自行编译。
2. Put it anywhere and run it. A blue arrows icon appears in the system tray (new icons may start in the `^` overflow area). / 放到任意目录运行，系统托盘出现蓝色双箭头图标（新图标可能先收在 `^` 隐藏区里）。
3. Open the menu, choose **Add Profile…**, and fill in the name and the two exits' gateway/DNS pairs. Repeat for each location. / 打开菜单选**添加 Profile…**，填入名字与两个出口的网关/DNS。每个常去的地点重复一次。
4. Optional: open **Settings…** and tick **Start at logon**. / 可选：打开**设置…**，勾上**开机自动启动**。

Requires Windows 10/11 with .NET Framework 4.x (in-box). / 需要 Windows 10/11 与系统自带的 .NET Framework 4.x。

## Build / 编译

The whole app is one C# file with no dependencies. Build it with the C# compiler that ships with Windows — no SDK or Visual Studio needed:

整个程序是一个零依赖的 C# 文件。用 Windows 自带的 C# 编译器即可编译，不需要 SDK 或 Visual Studio：

```
build.cmd
```

or manually / 或手工：

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:NetworkSwitch.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll NetworkSwitch.cs
```

## Config file / 配置文件

`%USERPROFILE%\.network-switch.conf` is plain text — easy to inspect or pre-seed by hand (the `APP|` lines are this edition's settings; the macOS edition ignores them):

`%USERPROFILE%\.network-switch.conf` 是纯文本，便于查看或手工预置（`APP|` 行是本版的设置项，macOS 版会忽略它们）：

```
# network-switch profiles (v2)
ACTIVE=Home
APP|AUTOSTART|1
APP|AUTOUPDATE|1
APP|PROXY_IP|172.20.10.1
APP|PROXY_PORT|7890
PROFILE|Home|DIRECT_GW|192.168.52.1
PROFILE|Home|DIRECT_DNS|192.168.52.1
PROFILE|Home|OVERSEAS_GW|192.168.52.2
PROFILE|Home|OVERSEAS_DNS|192.168.52.2
PROFILE|Office|DIRECT_GW|192.168.50.1
PROFILE|Office|DIRECT_DNS|192.168.50.1
PROFILE|Office|OVERSEAS_GW|192.168.50.2
PROFILE|Office|OVERSEAS_DNS|192.168.50.2
```

## Notes / 说明

- Switching pins the machine's current IP and mask statically with the chosen gateway/DNS (the same semantics as the macOS edition's manual setup). If you carry the laptop to a different network afterwards, use **Restore Automatic (DHCP)** first so it can pick up that network's own settings. / 切换会把本机当前 IP 与掩码按静态钉住，只换网关与 DNS（与 macOS 版的手动设置语义相同）。之后把笔记本带去另一个网络前，先点**恢复自动（DHCP）**，让它重新获取那边的设置。
- Brief detection misses on a flaky Wi-Fi stack no longer flash "Unknown": the last good state is kept for up to 60 seconds. / 无线协议栈偶尔抽风的几秒读空不再闪「未知」：60 秒内沿用上一次的好状态。
- Set `NETSWITCH_DEBUG=1` to get a diagnostic `detect.log` next to the exe. / 设环境变量 `NETSWITCH_DEBUG=1` 可在 exe 旁生成诊断日志 `detect.log`。

## Credits / 作者

Idea: Otakun · Coding: Dash

Sibling project / 姊妹项目：[swiftbar-network-switch](https://github.com/iam-otakun/swiftbar-network-switch)（macOS 版）

Changelog / 更新日志：[CHANGELOG.md](CHANGELOG.md)

## License / 许可

MIT — see [LICENSE](LICENSE). / MIT 许可，见 [LICENSE](LICENSE)。
