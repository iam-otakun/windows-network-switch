// 网络切换 / Network Switch (Windows tray edition) v1.2.4
// v1.2.4: the About dialog's GitHub link now reads "GitHub Repository"
// (in v1.1.0-v1.2.3 the raw URL was the link text) - the reference step
// of the unified About rolled out to all our Windows and macOS
// programs in this round.
// v1.2.3: the mutual exclusion is symmetric - while the measured
// gateway matches a profile exit (profile mode), the Proxy item is
// greyed out too; it stays clickable while the proxy is on (to switch
// it off) and on networks matching no profile (the hotspot case).
// v1.2.2: Proxy mode and profile switching are mutually exclusive -
// while the proxy is on, both exits of every profile are greyed out
// (the local gateway must stay as the LAN/hotspot gave it); Edit and
// Delete stay available.
// v1.2.1: the status line and tray tooltip now name the MEASURED
// profile (the one whose exit the current gateway matches); when the
// connection matches no profile at all (e.g. a phone hotspot) they
// show just "Unknown" with no profile name - "Home-Unknown" implied a
// relation to Home that does not exist.
// v1.2.0: Proxy toggle in the menu under the Profiles (checkmark =
// Windows system proxy on, pointing at the address set in Settings;
// Settings gains Proxy IP / Proxy Port fields, preset 172.20.10.1:7890
// for a phone-hotspot Clash share). Menu checkmarks are now driven by
// the MEASURED gateway per profile, not by the saved ACTIVE name: on a
// foreign network (e.g. a phone hotspot) nothing is checked, and after
// Restore Automatic (DHCP) a check appears only if the DHCP gateway
// actually matches a profile exit.
// v1.1.1: embedded application icon for the exe (same design as the tray
// icon: blue disc, two opposing white arrows); no functional changes.
// v1.1.0: About dialog (Idea: Otakun, Coding: Dash, GitHub link, version
// read from the assembly); Settings dialog with Start-at-logon (HKCU Run
// key) and automatic update checks against GitHub Releases (notify-only
// auto-check, click-to-update with self-replacing download); diagnostic
// logging now opt-in via NETSWITCH_DEBUG=1.
// v1.0.2 (field build, unreleased): last-good-state hysteresis so a
// momentary detection miss on a flaky Wi-Fi stack no longer flashes
// "Unknown" (field logs showed brief empty Detect() results on an
// otherwise healthy link, self-recovering at the next 10s tick).
// v1.0.1: diagnostic logging (detect.log next to the exe) to hunt a
// stale-tooltip bug; Detect() hardened with try/catch.
// Authors: Otakun and Dash  https://github.com/iam-otakun/windows-network-switch
//
// Windows system-tray counterpart of the macOS SwiftBar plugin
// (swiftbar-network-switch v2.3). Same architecture and the SAME config
// file format (~/.network-switch.conf in the user profile):
//
//   # network-switch profiles (v2)
//   ACTIVE=Home
//   PROFILE|Home|DIRECT_GW|192.168.52.1
//   PROFILE|Home|DIRECT_DNS|192.168.52.1
//   PROFILE|Home|OVERSEAS_GW|192.168.52.2
//   PROFILE|Home|OVERSEAS_DNS|192.168.52.2
//   APP|PROXY_IP|172.20.10.1
//   APP|PROXY_PORT|7890
//
// Behaviour mirrors the Mac version:
//  - Tray text shows "Profile-Mode", e.g. Home-出海.
//  - The active adapter is the one carrying the default route; the
//    machine's own IP and mask are detected automatically and kept.
//  - A switch pins IP+mask statically with the chosen gateway and DNS.
//  - Applying runs elevated (UAC) per switch, like the Mac version's
//    administrator-privileges prompt; cancelling UAC is reported
//    separately from a real failure, and failures show the system text.
//  - Profiles can be added / edited (rename included) / deleted from
//    the menu; at least one profile must remain.
//  - "Restore Automatic (DHCP)" returns address and DNS to automatic.
//  - UI language follows the system (zh -> Chinese, otherwise English);
//    NETSWITCH_LANG overrides.
//
// Build (in-box C# compiler, no SDK needed):
//   csc /target:winexe /out:NetworkSwitch.exe NetworkSwitch.cs
// Self-elevation helper mode (invoked by the app itself):
//   NetworkSwitch.exe --apply "<ifName>" static <ip> <mask> <gw> <dns>
//   NetworkSwitch.exe --apply "<ifName>" dhcp

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("网络切换 / Network Switch")]
[assembly: System.Reflection.AssemblyDescription("Switch the active network connection between two gateways on the same LAN from the system tray. Windows counterpart of swiftbar-network-switch.")]
[assembly: System.Reflection.AssemblyCompany("Otakun and Dash")]
[assembly: System.Reflection.AssemblyProduct("Network Switch")]
[assembly: System.Reflection.AssemblyVersion("1.2.4.0")]

namespace NetworkSwitch
{
    // ---------------- language ----------------
    static class L
    {
        public static bool Zh;
        public static string DIRECT, OVERSEAS, UNKNOWN, GW, AUTO, DHCP, QUIT;
        public static string EDIT, DEL, ADD, NOTCONF, NOTCONF_TIP, NOTCONF_LINE;
        public static string TIP_FMT, STATUS_FMT, EXIT_FMT, IFACE_FMT;
        public static string TIP_UNKNOWN_FMT, STATUS_UNKNOWN_FMT;
        public static string DLG_TITLE, NAME_PROMPT, NAME_LABEL;
        public static string CANCEL, SAVE, DELETE_BTN;
        public static string ABOUT, CLOSE, VERSION_FMT;
        public static string SETTINGS, AUTOSTART_TXT, AUTOUPDATE_TXT, CHECK_UPDATE;
        public static string PROXY, PROXY_IP, PROXY_PORT, BAD_PORT;
        public static string UPDATE_NONE, UPDATE_FOUND_Q, UPDATE_TITLE, UPDATE_FAIL_FMT, UPDATE_MENU_FMT, UPDATE_NOASSET;
        public static string SWITCH_FAIL, NO_IP_FMT, SWITCH_CANCELLED, SWITCH_ERR_FMT;
        public static string NOT_SAVED, NOT_ADDED, CANT_DELETE;
        public static string BAD_IP, NAME_EMPTY, NAME_PIPE, NAME_EXISTS_FMT, KEEP_ONE, DEL_Q_FMT;

        static L()
        {
            string lang = Environment.GetEnvironmentVariable("NETSWITCH_LANG");
            if (string.IsNullOrEmpty(lang)) lang = CultureInfo.CurrentUICulture.Name;
            Zh = lang != null && lang.StartsWith("zh");
            if (Zh)
            {
                DIRECT = "直连"; OVERSEAS = "出海"; UNKNOWN = "未知"; GW = "网关";
                AUTO = "自动（DHCP）"; DHCP = "恢复自动（DHCP）"; QUIT = "退出";
                EDIT = "编辑此 Profile…"; DEL = "删除此 Profile…"; ADD = "添加 Profile…";
                NOTCONF = "未配置"; NOTCONF_TIP = "还没有任何 Profile"; NOTCONF_LINE = "还没有 Profile，先添加一个";
                TIP_FMT = "当前：{0}-{1}，网关 {2}，DNS {3}，网卡 {4}（{5}）";
                STATUS_FMT = "当前：{0}-{1}（网关 {2}，DNS {3}）";
                TIP_UNKNOWN_FMT = "当前：{0}，网关 {1}，DNS {2}，网卡 {3}（{4}）";
                STATUS_UNKNOWN_FMT = "当前：{0}（网关 {1}，DNS {2}）";
                EXIT_FMT = "{0}（网关 {1}，DNS {2}）";
                IFACE_FMT = "网卡 {0}（{1}）";
                DLG_TITLE = "网络切换 · Profile";
                NAME_PROMPT = "Profile 名称（例如 Home、Office）";
                NAME_LABEL = "名称";
                CANCEL = "取消"; SAVE = "保存"; DELETE_BTN = "删除";
                ABOUT = "关于"; CLOSE = "关闭"; VERSION_FMT = "版本 {0}";
                SETTINGS = "设置"; AUTOSTART_TXT = "开机自动启动";
                AUTOUPDATE_TXT = "自动检查更新（仅提醒，不自动安装）";
                CHECK_UPDATE = "检查更新…";
                PROXY = "Proxy"; PROXY_IP = "代理 IP"; PROXY_PORT = "代理端口";
                BAD_PORT = "端口不对，应为 1–65535 之间的数字。";
                UPDATE_NONE = "已是最新版本（{0}）";
                UPDATE_FOUND_Q = "发现新版本 {0}（当前 {1}），现在更新吗？";
                UPDATE_TITLE = "网络切换 · 更新";
                UPDATE_FAIL_FMT = "检查更新失败：{0}";
                UPDATE_MENU_FMT = "有新版本 {0}，点击更新";
                UPDATE_NOASSET = "新版本没有附带程序文件，已为你打开下载页面。";
                SWITCH_FAIL = "切换失败";
                NO_IP_FMT = "当前网卡（{0}）没有 IP 地址，无法切换。";
                SWITCH_CANCELLED = "在 UAC 弹窗点了取消，本次没有切换。";
                SWITCH_ERR_FMT = "切换命令执行失败，网络没有改动。系统报错：{0}";
                NOT_SAVED = "未保存"; NOT_ADDED = "未添加"; CANT_DELETE = "不能删除";
                BAD_IP = "有地址格式不对，应为 IPv4 形式，例如 192.168.1.1。";
                NAME_EMPTY = "Profile 名称不能为空。";
                NAME_PIPE = "Profile 名称不能包含 | 字符。";
                NAME_EXISTS_FMT = "已经有一个叫「{0}」的 Profile 了。";
                KEEP_ONE = "至少要保留一个 Profile。";
                DEL_Q_FMT = "删除 Profile「{0}」？";
            }
            else
            {
                DIRECT = "Direct"; OVERSEAS = "Overseas"; UNKNOWN = "Unknown"; GW = "Gateway";
                AUTO = "Automatic (DHCP)"; DHCP = "Restore Automatic (DHCP)"; QUIT = "Quit";
                EDIT = "Edit this Profile..."; DEL = "Delete this Profile..."; ADD = "Add Profile...";
                NOTCONF = "Not Configured"; NOTCONF_TIP = "No Profiles yet"; NOTCONF_LINE = "No Profiles yet - add one first";
                TIP_FMT = "Current: {0}-{1}, gateway {2}, DNS {3}, adapter {4} ({5})";
                STATUS_FMT = "Current: {0}-{1} (gateway {2}, DNS {3})";
                TIP_UNKNOWN_FMT = "Current: {0}, gateway {1}, DNS {2}, adapter {3} ({4})";
                STATUS_UNKNOWN_FMT = "Current: {0} (gateway {1}, DNS {2})";
                EXIT_FMT = "{0} (gateway {1}, DNS {2})";
                IFACE_FMT = "Adapter {0} ({1})";
                DLG_TITLE = "Network Switch - Profile";
                NAME_PROMPT = "Profile name (e.g. Home, Office)";
                NAME_LABEL = "Name";
                CANCEL = "Cancel"; SAVE = "Save"; DELETE_BTN = "Delete";
                ABOUT = "About"; CLOSE = "Close"; VERSION_FMT = "Version {0}";
                SETTINGS = "Settings"; AUTOSTART_TXT = "Start at logon";
                AUTOUPDATE_TXT = "Check for updates automatically (notify only)";
                CHECK_UPDATE = "Check for Updates...";
                PROXY = "Proxy"; PROXY_IP = "Proxy IP"; PROXY_PORT = "Proxy Port";
                BAD_PORT = "The port must be a number between 1 and 65535.";
                UPDATE_NONE = "You are up to date ({0})";
                UPDATE_FOUND_Q = "Version {0} is available (current {1}). Update now?";
                UPDATE_TITLE = "Network Switch - Update";
                UPDATE_FAIL_FMT = "Update check failed: {0}";
                UPDATE_MENU_FMT = "Version {0} available - click to update";
                UPDATE_NOASSET = "The new release has no program file attached; opened the download page instead.";
                SWITCH_FAIL = "Switch Failed";
                NO_IP_FMT = "The current adapter ({0}) has no IP address, so switching is not possible.";
                SWITCH_CANCELLED = "Cancelled at the UAC prompt - nothing was switched.";
                SWITCH_ERR_FMT = "The switch command failed, so the network was left unchanged. System error: {0}";
                NOT_SAVED = "Not Saved"; NOT_ADDED = "Not Added"; CANT_DELETE = "Cannot Delete";
                BAD_IP = "One of the addresses is not in IPv4 form, e.g. 192.168.1.1.";
                NAME_EMPTY = "The Profile name cannot be empty.";
                NAME_PIPE = "The Profile name cannot contain the | character.";
                NAME_EXISTS_FMT = "A Profile named {0} already exists.";
                KEEP_ONE = "At least one Profile must remain.";
                DEL_Q_FMT = "Delete the Profile {0}?";
            }
        }
    }

    // ---------------- config ----------------
    class Profile
    {
        public string Name, DGw, DDns, OGw, ODns;
    }

    static class Conf
    {
        public static string Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".network-switch.conf");
        public static List<Profile> Profiles = new List<Profile>();
        public static string Active = "";
        public static bool AutoStart = false;
        public static bool AutoUpdate = true;
        public static string LastCheck = "";
        public static string ProxyIp = "172.20.10.1";
        public static string ProxyPort = "7890";

        public static void Load()
        {
            Profiles = new List<Profile>();
            Active = "";
            AutoStart = false; AutoUpdate = true; LastCheck = "";
            ProxyIp = "172.20.10.1"; ProxyPort = "7890";
            if (!File.Exists(Path)) return;
            var map = new Dictionary<string, Profile>();
            var order = new List<string>();
            foreach (string raw in File.ReadAllLines(Path))
            {
                string line = raw.Trim();
                if (line.StartsWith("ACTIVE="))
                {
                    Active = line.Substring("ACTIVE=".Length).Trim();
                }
                else if (line.StartsWith("APP|"))
                {
                    string[] ap = line.Split('|');
                    if (ap.Length >= 3)
                    {
                        if (ap[1] == "AUTOSTART") AutoStart = ap[2] == "1";
                        else if (ap[1] == "AUTOUPDATE") AutoUpdate = ap[2] == "1";
                        else if (ap[1] == "LASTCHECK") LastCheck = ap[2];
                        else if (ap[1] == "PROXY_IP") ProxyIp = ap[2];
                        else if (ap[1] == "PROXY_PORT") ProxyPort = ap[2];
                    }
                }
                else if (line.StartsWith("PROFILE|"))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length < 4) continue;
                    string name = parts[1], key = parts[2], val = parts[3];
                    Profile p;
                    if (!map.TryGetValue(name, out p))
                    {
                        p = new Profile(); p.Name = name;
                        p.DGw = ""; p.DDns = ""; p.OGw = ""; p.ODns = "";
                        map[name] = p; order.Add(name);
                    }
                    if (key == "DIRECT_GW") p.DGw = val;
                    else if (key == "DIRECT_DNS") p.DDns = val;
                    else if (key == "OVERSEAS_GW") p.OGw = val;
                    else if (key == "OVERSEAS_DNS") p.ODns = val;
                }
            }
            foreach (string n in order) Profiles.Add(map[n]);
            if (IdxOf(Active) < 0 && Profiles.Count > 0) Active = Profiles[0].Name;
        }

        public static void Save()
        {
            var lines = new List<string>();
            lines.Add("# network-switch profiles (v2)");
            lines.Add("ACTIVE=" + Active);
            lines.Add("APP|AUTOSTART|" + (AutoStart ? "1" : "0"));
            lines.Add("APP|AUTOUPDATE|" + (AutoUpdate ? "1" : "0"));
            lines.Add("APP|PROXY_IP|" + ProxyIp);
            lines.Add("APP|PROXY_PORT|" + ProxyPort);
            if (LastCheck.Length > 0) lines.Add("APP|LASTCHECK|" + LastCheck);
            foreach (Profile p in Profiles)
            {
                lines.Add("PROFILE|" + p.Name + "|DIRECT_GW|" + p.DGw);
                lines.Add("PROFILE|" + p.Name + "|DIRECT_DNS|" + p.DDns);
                lines.Add("PROFILE|" + p.Name + "|OVERSEAS_GW|" + p.OGw);
                lines.Add("PROFILE|" + p.Name + "|OVERSEAS_DNS|" + p.ODns);
            }
            string tmp = Path + ".tmp";
            File.WriteAllLines(tmp, lines.ToArray());
            if (File.Exists(Path)) File.Delete(Path);
            File.Move(tmp, Path);
        }

        public static int IdxOf(string name)
        {
            for (int i = 0; i < Profiles.Count; i++)
                if (Profiles[i].Name == name) return i;
            return -1;
        }
    }

    // ---------------- network detection ----------------
    class NetState
    {
        public string IfName = "";
        public string IP = "";
        public string Mask = "";
        public string Gateway = "";
        public string Dns = "";
        public bool Ok = false;
    }

    static class Log
    {
        static string FilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "detect.log");
        static bool Enabled = Environment.GetEnvironmentVariable("NETSWITCH_DEBUG") == "1";
        public static void Write(string msg)
        {
            if (!Enabled) return;
            try { File.AppendAllText(FilePath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\r\n"); } catch { }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPFORWARDROW
    {
        public uint dwForwardDest, dwForwardMask, dwForwardPolicy, dwForwardNextHop;
        public uint dwForwardIfIndex, dwForwardType, dwForwardProto, dwForwardAge;
        public uint dwForwardNextHopAS;
        public uint dwForwardMetric1, dwForwardMetric2, dwForwardMetric3, dwForwardMetric4, dwForwardMetric5;
    }

    static class Net
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        static extern uint GetBestRoute(uint dwDestAddr, uint dwSourceAddr, out MIB_IPFORWARDROW pBestRoute);

        static string IpFromUint(uint v)
        {
            return new IPAddress(BitConverter.GetBytes(v)).ToString();
        }

        static NetState lastGood;
        static DateTime lastGoodAt = DateTime.MinValue;

        public static NetState Detect()
        {
            NetState st;
            try { st = DetectCore(); }
            catch (Exception ex)
            {
                Log.Write("Detect EXC " + ex.GetType().Name + ": " + ex.Message);
                st = new NetState();
            }
            Log.Write("Detect gw=" + st.Gateway + " if=" + st.IfName + " ip=" + st.IP + " dns=" + st.Dns + " ok=" + st.Ok);
            if (st.Ok)
            {
                lastGood = st;
                lastGoodAt = DateTime.Now;
                return st;
            }
            // Hysteresis: a momentary detection miss (flaky Wi-Fi stack)
            // must not flash "Unknown" - keep the last good state for 60s.
            if (lastGood != null && (DateTime.Now - lastGoodAt).TotalSeconds < 60)
            {
                Log.Write("Detect miss - keeping last good state");
                return lastGood;
            }
            return st;
        }

        static NetState DetectCore()
        {
            var st = new NetState();
            MIB_IPFORWARDROW row;
            uint rc = GetBestRoute(0, 0, out row);
            if (rc != 0) return st;
            int ifIndex = (int)row.dwForwardIfIndex;
            st.Gateway = IpFromUint(row.dwForwardNextHop);
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                IPInterfaceProperties props = ni.GetIPProperties();
                IPv4InterfaceProperties v4 = null;
                try { v4 = props.GetIPv4Properties(); } catch { }
                if (v4 == null || v4.Index != ifIndex) continue;
                st.IfName = ni.Name;
                foreach (UnicastIPAddressInformation u in props.UnicastAddresses)
                {
                    if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        st.IP = u.Address.ToString();
                        try { st.Mask = u.IPv4Mask.ToString(); } catch { st.Mask = ""; }
                        break;
                    }
                }
                foreach (IPAddress d in props.DnsAddresses)
                {
                    if (d.AddressFamily == AddressFamily.InterNetwork) { st.Dns = d.ToString(); break; }
                }
                st.Ok = st.IP.Length > 0;
                return st;
            }
            return st;
        }

        public static bool ValidIp(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            string[] parts = s.Split('.');
            if (parts.Length != 4) return false;
            foreach (string part in parts)
            {
                if (part.Length == 0 || part.Length > 3) return false;
                foreach (char c in part) if (!char.IsDigit(c)) return false;
                int v = int.Parse(part);
                if (v < 0 || v > 255) return false;
            }
            return true;
        }
    }

    // ---------------- elevated apply helper ----------------
    static class Apply
    {
        public static string LogPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "network-switch-apply.log");

        // Runs INSIDE the elevated instance. Returns process exit code.
        public static int Run(string[] args)
        {
            // args: --apply <ifName> static <ip> <mask> <gw> <dns> | --apply <ifName> dhcp
            string ifName = args[1];
            var cmds = new List<string>();
            if (args[2] == "dhcp")
            {
                cmds.Add("interface ip set address name=\"" + ifName + "\" source=dhcp");
                cmds.Add("interface ip set dnsservers name=\"" + ifName + "\" source=dhcp");
            }
            else
            {
                cmds.Add("interface ip set address name=\"" + ifName + "\" source=static address=" + args[3] + " mask=" + args[4] + " gateway=" + args[5]);
                cmds.Add("interface ip set dnsservers name=\"" + ifName + "\" source=static address=" + args[6] + " validate=no");
            }
            var log = new List<string>();
            int fail = 0;
            foreach (string c in cmds)
            {
                var psi = new ProcessStartInfo("netsh", c);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                var p = Process.Start(psi);
                string outText = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit();
                log.Add("netsh " + c);
                log.Add(outText.Trim());
                if (p.ExitCode != 0) fail = 2;
            }
            try { File.WriteAllLines(LogPath, log.ToArray()); } catch { }
            return fail;
        }

        // Called from the (unelevated) tray app. Shows the same alerts as the Mac version.
        public static void Invoke(string ifName, bool dhcp, string ip, string mask, string gw, string dns)
        {
            string argStr = dhcp
                ? "--apply \"" + ifName + "\" dhcp"
                : "--apply \"" + ifName + "\" static " + ip + " " + mask + " " + gw + " " + dns;
            var psi = new ProcessStartInfo(Application.ExecutablePath, argStr);
            psi.Verb = "runas";
            psi.UseShellExecute = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process p;
            try
            {
                p = Process.Start(psi);
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) // ERROR_CANCELLED: user pressed No at UAC
                    MessageBox.Show(L.SWITCH_CANCELLED, L.SWITCH_FAIL, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show(string.Format(L.SWITCH_ERR_FMT, ex.Message), L.SWITCH_FAIL, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            p.WaitForExit();
            Thread.Sleep(800); // let the new route settle before state is re-read
            if (p.ExitCode != 0)
            {
                string detail = "";
                try
                {
                    string[] lines = File.ReadAllLines(LogPath);
                    foreach (string s in lines) if (s.Trim().Length > 0) detail = s.Trim(); // last non-empty line
                }
                catch { }
                if (detail.Length == 0) detail = "netsh exit " + p.ExitCode;
                MessageBox.Show(string.Format(L.SWITCH_ERR_FMT, detail), L.SWITCH_FAIL, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ---------------- autostart + updates ----------------
    static class Startup
    {
        public static void Apply(bool enabled)
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;
                if (enabled) key.SetValue("NetworkSwitch", "\"" + Application.ExecutablePath + "\"");
                else key.DeleteValue("NetworkSwitch", false);
                key.Close();
            }
            catch { }
        }
    }

    // ---------------- system proxy (WinINET, per-user) ----------------
    static class ProxyCtl
    {
        const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        const int INTERNET_OPTION_REFRESH = 37;

        [DllImport("wininet.dll", SetLastError = true)]
        static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        public static string ServerString()
        {
            return Conf.ProxyIp + ":" + Conf.ProxyPort;
        }

        // "On" means: the system proxy is enabled AND points at the
        // address configured in Settings. A proxy set by another app
        // (different server string) does not count as on here.
        public static bool IsOn()
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyPath, false);
                if (key == null) return false;
                int en = Convert.ToInt32(key.GetValue("ProxyEnable", 0));
                string srv = key.GetValue("ProxyServer", "") as string;
                key.Close();
                return en == 1 && srv == ServerString();
            }
            catch { return false; }
        }

        public static void Set(bool on)
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyPath, true);
                if (key == null) return;
                if (on)
                {
                    key.SetValue("ProxyServer", ServerString());
                    key.SetValue("ProxyEnable", 1, Microsoft.Win32.RegistryValueKind.DWord);
                }
                else
                {
                    key.SetValue("ProxyEnable", 0, Microsoft.Win32.RegistryValueKind.DWord);
                }
                key.Close();
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            }
            catch { }
        }
    }

    class ReleaseInfo
    {
        public Version Version;
        public string Tag = "";
        public string AssetUrl = "";
        public string PageUrl = "";
    }

    static class Updater
    {
        public static string ApiUrl = "https://api.github.com/repos/iam-otakun/windows-network-switch/releases/latest";
        public static string ReleasesPage = "https://github.com/iam-otakun/windows-network-switch/releases";

        public static Version CurrentVersion
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public static string CurrentText
        {
            get { var v = CurrentVersion; return v.Major + "." + v.Minor + "." + v.Build; }
        }

        public static ReleaseInfo FetchLatest()
        {
            System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072; // TLS 1.2
            var wc = new System.Net.WebClient();
            wc.Headers["User-Agent"] = "NetworkSwitch/" + CurrentText;
            string json = wc.DownloadString(ApiUrl);
            var info = new ReleaseInfo();
            info.Tag = Extract(json, "\"tag_name\"");
            string ver = info.Tag.StartsWith("v") ? info.Tag.Substring(1) : info.Tag;
            Version parsed;
            if (!Version.TryParse(ver, out parsed)) return null;
            info.Version = parsed;
            info.PageUrl = Extract(json, "\"html_url\"");
            int idx = json.IndexOf("\"name\": \"NetworkSwitch.exe\"");
            if (idx < 0) idx = json.IndexOf("\"name\":\"NetworkSwitch.exe\"");
            if (idx >= 0)
            {
                int u = json.IndexOf("\"browser_download_url\"", idx);
                if (u >= 0) info.AssetUrl = Extract(json.Substring(u), "\"browser_download_url\"");
            }
            return info;
        }

        static string Extract(string json, string key)
        {
            int i = json.IndexOf(key);
            if (i < 0) return "";
            int colon = json.IndexOf(':', i + key.Length);
            if (colon < 0) return "";
            int q1 = json.IndexOf('"', colon);
            if (q1 < 0) return "";
            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0) return "";
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }

        // Downloads the new exe, then hands over to a small .cmd that waits
        // for this process to exit, swaps the file and relaunches it.
        public static void DownloadAndSwap(string url)
        {
            string tmpExe = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NetworkSwitch-new.exe");
            System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
            var wc = new System.Net.WebClient();
            wc.Headers["User-Agent"] = "NetworkSwitch/" + CurrentText;
            wc.DownloadFile(url, tmpExe);
            string cmdPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "network-switch-update.cmd");
            string exe = Application.ExecutablePath;
            var lines = new List<string>();
            lines.Add("@echo off");
            lines.Add("set /a n=0");
            lines.Add(":retry");
            lines.Add("copy /Y \"" + tmpExe + "\" \"" + exe + "\" >nul 2>&1 && goto done");
            lines.Add("set /a n+=1");
            lines.Add("if %n% GEQ 30 goto done");
            lines.Add("timeout /t 1 >nul");
            lines.Add("goto retry");
            lines.Add(":done");
            lines.Add("start \"\" \"" + exe + "\"");
            lines.Add("del \"%~f0\"");
            File.WriteAllLines(cmdPath, lines.ToArray());
            var psi = new ProcessStartInfo("cmd", "/c \"" + cmdPath + "\"");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            Process.Start(psi);
        }
    }

    // ---------------- add / edit dialog ----------------
    class ProfileForm : Form
    {
        TextBox tbName, tbDGw, tbDDns, tbOGw, tbODns;
        public Profile Result;

        public ProfileForm(Profile existing)
        {
            Text = L.DLG_TITLE;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(360, 268);
            Font = new Font("Microsoft YaHei UI", 9f);

            var lblPrompt = new Label();
            lblPrompt.Text = L.NAME_PROMPT;
            lblPrompt.Location = new Point(14, 10);
            lblPrompt.Size = new Size(332, 18);
            Controls.Add(lblPrompt);

            tbName = AddRow(L.NAME_LABEL, 34, existing == null ? "" : existing.Name);
            tbDGw = AddRow(L.DIRECT + " · " + L.GW, 68, existing == null ? "" : existing.DGw);
            tbDDns = AddRow(L.DIRECT + " · DNS", 102, existing == null ? "" : existing.DDns);
            tbOGw = AddRow(L.OVERSEAS + " · " + L.GW, 136, existing == null ? "" : existing.OGw);
            tbODns = AddRow(L.OVERSEAS + " · DNS", 170, existing == null ? "" : existing.ODns);

            var btnOk = new Button();
            btnOk.Text = L.SAVE;
            btnOk.Location = new Point(180, 216);
            btnOk.Size = new Size(80, 28);
            btnOk.Click += delegate { OnSave(existing); };
            Controls.Add(btnOk);

            var btnCancel = new Button();
            btnCancel.Text = L.CANCEL;
            btnCancel.Location = new Point(268, 216);
            btnCancel.Size = new Size(80, 28);
            btnCancel.DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        TextBox AddRow(string label, int y, string value)
        {
            var lbl = new Label();
            lbl.Text = label;
            lbl.Location = new Point(14, y + 3);
            lbl.Size = new Size(110, 18);
            Controls.Add(lbl);
            var tb = new TextBox();
            tb.Text = value;
            tb.Location = new Point(128, y);
            tb.Size = new Size(218, 24);
            Controls.Add(tb);
            return tb;
        }

        void OnSave(Profile existing)
        {
            string name = tbName.Text.Trim();
            string title = existing == null ? L.NOT_ADDED : L.NOT_SAVED;
            if (name.Length == 0)
            {
                MessageBox.Show(L.NAME_EMPTY, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (name.Contains("|"))
            {
                MessageBox.Show(L.NAME_PIPE, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int dup = Conf.IdxOf(name);
            if (dup >= 0 && (existing == null || Conf.Profiles[dup] != existing))
            {
                MessageBox.Show(string.Format(L.NAME_EXISTS_FMT, name), title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string[] vals = { tbDGw.Text.Trim(), tbDDns.Text.Trim(), tbOGw.Text.Trim(), tbODns.Text.Trim() };
            foreach (string v in vals)
            {
                if (!Net.ValidIp(v))
                {
                    MessageBox.Show(L.BAD_IP, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            Result = new Profile();
            Result.Name = name;
            Result.DGw = vals[0]; Result.DDns = vals[1]; Result.OGw = vals[2]; Result.ODns = vals[3];
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // ---------------- settings dialog ----------------
    class SettingsForm : Form
    {
        CheckBox cbAutoStart, cbAutoUpdate;
        TextBox tbProxyIp, tbProxyPort;
        public bool CheckRequested = false;

        public SettingsForm()
        {
            Text = L.SETTINGS;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(360, 236);
            Font = new Font("Microsoft YaHei UI", 9f);

            cbAutoStart = new CheckBox();
            cbAutoStart.Text = L.AUTOSTART_TXT;
            cbAutoStart.Location = new Point(16, 14);
            cbAutoStart.Size = new Size(328, 22);
            cbAutoStart.Checked = Conf.AutoStart;
            Controls.Add(cbAutoStart);

            cbAutoUpdate = new CheckBox();
            cbAutoUpdate.Text = L.AUTOUPDATE_TXT;
            cbAutoUpdate.Location = new Point(16, 42);
            cbAutoUpdate.Size = new Size(328, 22);
            cbAutoUpdate.Checked = Conf.AutoUpdate;
            Controls.Add(cbAutoUpdate);

            tbProxyIp = AddRow(L.PROXY_IP, 76, Conf.ProxyIp);
            tbProxyPort = AddRow(L.PROXY_PORT, 106, Conf.ProxyPort);

            var btnCheck = new Button();
            btnCheck.Text = L.CHECK_UPDATE;
            btnCheck.Location = new Point(16, 146);
            btnCheck.Size = new Size(140, 28);
            btnCheck.Click += delegate { if (!SaveSettings()) return; CheckRequested = true; DialogResult = DialogResult.OK; Close(); };
            Controls.Add(btnCheck);

            var btnOk = new Button();
            btnOk.Text = L.SAVE;
            btnOk.Location = new Point(176, 192);
            btnOk.Size = new Size(80, 28);
            btnOk.Click += delegate { if (!SaveSettings()) return; DialogResult = DialogResult.OK; Close(); };
            Controls.Add(btnOk);

            var btnCancel = new Button();
            btnCancel.Text = L.CANCEL;
            btnCancel.Location = new Point(264, 192);
            btnCancel.Size = new Size(80, 28);
            btnCancel.DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        TextBox AddRow(string label, int y, string value)
        {
            var lbl = new Label();
            lbl.Text = label;
            lbl.Location = new Point(16, y + 3);
            lbl.Size = new Size(100, 18);
            Controls.Add(lbl);
            var tb = new TextBox();
            tb.Text = value;
            tb.Location = new Point(120, y);
            tb.Size = new Size(224, 24);
            Controls.Add(tb);
            return tb;
        }

        // Returns false (and keeps the dialog open) on invalid input.
        bool SaveSettings()
        {
            string ip = tbProxyIp.Text.Trim();
            string port = tbProxyPort.Text.Trim();
            if (!Net.ValidIp(ip))
            {
                MessageBox.Show(L.BAD_IP, L.SETTINGS, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            int portNum;
            if (!int.TryParse(port, out portNum) || portNum < 1 || portNum > 65535)
            {
                MessageBox.Show(L.BAD_PORT, L.SETTINGS, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            // If our proxy is currently switched on, re-point it at the
            // new address right away so the menu check stays truthful.
            bool wasOn = ProxyCtl.IsOn();
            Conf.AutoStart = cbAutoStart.Checked;
            Conf.AutoUpdate = cbAutoUpdate.Checked;
            Conf.ProxyIp = ip;
            Conf.ProxyPort = port;
            Conf.Save();
            Startup.Apply(Conf.AutoStart);
            if (wasOn) ProxyCtl.Set(true);
            return true;
        }
    }

    // ---------------- about dialog ----------------
    class AboutForm : Form
    {
        public AboutForm()
        {
            Text = L.ABOUT;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(340, 196);
            Font = new Font("Microsoft YaHei UI", 9f);

            var title = new Label();
            title.Text = L.Zh ? "网络切换 / Network Switch" : "Network Switch";
            title.Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold);
            title.Location = new Point(16, 14);
            title.Size = new Size(308, 24);
            Controls.Add(title);

            var ver = new Label();
            ver.Text = string.Format(L.VERSION_FMT, Updater.CurrentText);
            ver.Location = new Point(16, 46);
            ver.Size = new Size(308, 18);
            Controls.Add(ver);

            var idea = new Label();
            idea.Text = "Idea: Otakun";
            idea.Location = new Point(16, 78);
            idea.Size = new Size(308, 18);
            Controls.Add(idea);

            var coding = new Label();
            coding.Text = "Coding: Dash";
            coding.Location = new Point(16, 100);
            coding.Size = new Size(308, 18);
            Controls.Add(coding);

            var link = new LinkLabel();
            link.Text = "GitHub Repository";
            link.Location = new Point(16, 128);
            link.Size = new Size(308, 18);
            link.LinkClicked += delegate { Process.Start("https://github.com/iam-otakun/windows-network-switch"); };
            Controls.Add(link);

            var btn = new Button();
            btn.Text = L.CLOSE;
            btn.Location = new Point(244, 158);
            btn.Size = new Size(80, 28);
            btn.DialogResult = DialogResult.OK;
            Controls.Add(btn);
            AcceptButton = btn;
            CancelButton = btn;
        }
    }

    // ---------------- tray app ----------------
    class TrayApp : ApplicationContext
    {
        NotifyIcon tray;
        ContextMenuStrip menu;
        System.Windows.Forms.Timer timer;
        NetState state;
        ReleaseInfo pendingUpdate;
        bool balloonPending = false;

        public TrayApp()
        {
            Conf.Load();
            Startup.Apply(Conf.AutoStart);
            state = Net.Detect();
            tray = new NotifyIcon();
            tray.Icon = MakeIcon();
            tray.Visible = true;
            tray.Text = TrayText();
            menu = new ContextMenuStrip();
            menu.Opening += delegate { Rebuild(); };
            tray.ContextMenuStrip = menu;
            tray.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    // NotifyIcon shows the context menu on right-click natively;
                    // mirror the Mac menu-bar behaviour on left-click too.
                    var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (mi != null) mi.Invoke(tray, null);
                }
            };
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 10000; // echo of the SwiftBar 10s refresh
            timer.Tick += delegate
            {
                Log.Write("tick");
                state = Net.Detect();
                tray.Text = TrayText();
                Log.Write("text set: " + tray.Text);
                if (balloonPending && pendingUpdate != null)
                {
                    balloonPending = false;
                    tray.ShowBalloonTip(6000, L.UPDATE_TITLE,
                        string.Format(L.UPDATE_MENU_FMT, pendingUpdate.Tag), ToolTipIcon.Info);
                }
            };
            timer.Start();
            Rebuild();
            var once = new System.Windows.Forms.Timer();
            once.Interval = 15000;
            once.Tick += delegate { once.Stop(); ScheduleAutoCheck(); };
            once.Start();
        }

        void ScheduleAutoCheck()
        {
            if (!Conf.AutoUpdate) return;
            string today = DateTime.Now.ToString("yyyyMMdd");
            if (Conf.LastCheck == today) return;
            var th = new Thread(delegate()
            {
                try
                {
                    var info = Updater.FetchLatest();
                    Conf.LastCheck = today;
                    Conf.Save();
                    if (info != null && info.Version > Updater.CurrentVersion)
                    {
                        pendingUpdate = info;
                        balloonPending = true;
                    }
                }
                catch { }
            });
            th.IsBackground = true;
            th.Start();
        }

        static Icon MakeIcon()
        {
            var bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                {
                    g.FillEllipse(brush, 1, 1, 30, 30);
                }
                using (var pen = new Pen(Color.White, 2.4f))
                {
                    // right-pointing arrow (upper)
                    g.DrawLine(pen, 9, 13, 22, 13);
                    g.DrawLine(pen, 18.5f, 9, 22.5f, 13);
                    g.DrawLine(pen, 18.5f, 17, 22.5f, 13);
                    // left-pointing arrow (lower)
                    g.DrawLine(pen, 23, 20, 10, 20);
                    g.DrawLine(pen, 13.5f, 16, 9.5f, 20);
                    g.DrawLine(pen, 13.5f, 24, 9.5f, 20);
                }
            }
            Icon icon = Icon.FromHandle(bmp.GetHicon());
            return icon;
        }

        // Measured mode of one profile: which of its exits (if any) the
        // current gateway actually is.
        string ModeFor(Profile p)
        {
            if (state.Gateway == p.DGw) return L.DIRECT;
            if (state.Gateway == p.OGw) return L.OVERSEAS;
            return L.UNKNOWN;
        }

        // The profile the current connection actually belongs to, by
        // measured gateway - or null when it matches no profile at all
        // (e.g. a phone hotspot). The status line names this profile,
        // never the saved ACTIVE one, so "Home-Unknown" cannot appear.
        Profile MeasuredProfile()
        {
            foreach (Profile p in Conf.Profiles)
                if (ModeFor(p) != L.UNKNOWN) return p;
            return null;
        }

        string TrayText()
        {
            string t;
            if (Conf.Profiles.Count == 0)
                t = L.NOTCONF + " - " + L.NOTCONF_TIP;
            else
            {
                string gw = state.Gateway.Length > 0 ? state.Gateway : "-";
                string dns = state.Dns.Length > 0 ? state.Dns : L.AUTO;
                string ifn = state.IfName.Length > 0 ? state.IfName : "-";
                string ip = state.IP.Length > 0 ? state.IP : "-";
                Profile mp = MeasuredProfile();
                if (mp != null)
                    t = string.Format(L.TIP_FMT, mp.Name, ModeFor(mp), gw, dns, ifn, ip);
                else
                    t = string.Format(L.TIP_UNKNOWN_FMT, L.UNKNOWN, gw, dns, ifn, ip);
            }
            if (t.Length >= 64) t = t.Substring(0, 63); // NotifyIcon.Text limit
            return t;
        }

        void Rebuild()
        {
            state = Net.Detect();
            tray.Text = TrayText();
            menu.Items.Clear();
            if (Conf.Profiles.Count == 0)
            {
                var none = new ToolStripMenuItem(L.NOTCONF_LINE);
                none.Enabled = false;
                menu.Items.Add(none);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(MakeItem(L.ADD, delegate { AddProfile(); }));
                menu.Items.Add(MakeItem(L.SETTINGS + "…", delegate { ShowSettings(); }));
                menu.Items.Add(MakeItem(L.ABOUT, delegate { ShowAbout(); }));
                menu.Items.Add(MakeItem(L.QUIT, delegate { Quit(); }));
                return;
            }

            Profile mp = MeasuredProfile();
            string status;
            if (mp != null)
                status = string.Format(L.STATUS_FMT, mp.Name, ModeFor(mp),
                    state.Gateway.Length > 0 ? state.Gateway : "-",
                    state.Dns.Length > 0 ? state.Dns : L.AUTO);
            else
                status = string.Format(L.STATUS_UNKNOWN_FMT, L.UNKNOWN,
                    state.Gateway.Length > 0 ? state.Gateway : "-",
                    state.Dns.Length > 0 ? state.Dns : L.AUTO);
            var stItem = new ToolStripMenuItem(status);
            stItem.Enabled = false;
            menu.Items.Add(stItem);
            var ifItem = new ToolStripMenuItem(string.Format(L.IFACE_FMT,
                state.IfName.Length > 0 ? state.IfName : "-", state.IP.Length > 0 ? state.IP : "-"));
            ifItem.Enabled = false;
            menu.Items.Add(ifItem);
            menu.Items.Add(new ToolStripSeparator());

            if (pendingUpdate != null)
            {
                ReleaseInfo pu = pendingUpdate;
                var upItem = new ToolStripMenuItem(string.Format(L.UPDATE_MENU_FMT, pu.Tag));
                upItem.Click += delegate { RunUpdateFlow(pu); };
                menu.Items.Add(upItem);
                menu.Items.Add(new ToolStripSeparator());
            }

            bool proxyOn = ProxyCtl.IsOn();
            foreach (Profile p in Conf.Profiles)
            {
                Profile prof = p;
                // Checkmarks follow the MEASURED gateway for this profile,
                // never the saved ACTIVE name: on a foreign network (phone
                // hotspot, DHCP after Restore Automatic, ...) nothing is
                // checked unless the gateway really matches an exit.
                string pMode = ModeFor(prof);
                var item = new ToolStripMenuItem(prof.Name);
                item.Checked = pMode != L.UNKNOWN;
                // Proxy mode and gateway switching are mutually exclusive:
                // with the proxy on, the connection borrows another
                // device's Clash and the local gateway must stay as the
                // LAN/hotspot gave it - pinning a profile gateway would
                // contradict that, so both exits are greyed out. Edit and
                // Delete stay available; they do not touch the network.
                var dItem = new ToolStripMenuItem(string.Format(L.EXIT_FMT, L.DIRECT, prof.DGw, prof.DDns));
                dItem.Checked = pMode == L.DIRECT;
                dItem.Enabled = !proxyOn;
                dItem.Click += delegate { SwitchTo(prof, true); };
                item.DropDownItems.Add(dItem);
                var oItem = new ToolStripMenuItem(string.Format(L.EXIT_FMT, L.OVERSEAS, prof.OGw, prof.ODns));
                oItem.Checked = pMode == L.OVERSEAS;
                oItem.Enabled = !proxyOn;
                oItem.Click += delegate { SwitchTo(prof, false); };
                item.DropDownItems.Add(oItem);
                item.DropDownItems.Add(new ToolStripSeparator());
                var eItem = new ToolStripMenuItem(L.EDIT);
                eItem.Click += delegate { EditProfile(prof); };
                item.DropDownItems.Add(eItem);
                var delItem = new ToolStripMenuItem(L.DEL);
                delItem.Click += delegate { DeleteProfile(prof); };
                item.DropDownItems.Add(delItem);
                menu.Items.Add(item);
            }

            var proxyItem = new ToolStripMenuItem(L.PROXY);
            proxyItem.Checked = proxyOn;
            // The exclusion runs both ways: while the measured gateway
            // matches a profile exit, the connection is in profile mode
            // and the Proxy cannot be switched on (the proxy address
            // belongs to a foreign network, e.g. the phone hotspot, and
            // is not even reachable from here). Restore Automatic (DHCP)
            // is the way back to neutral. While the proxy is ON the item
            // itself always stays enabled - it is how you switch it off.
            proxyItem.Enabled = proxyOn || MeasuredProfile() == null;
            proxyItem.Click += delegate { ToggleProxy(); };
            menu.Items.Add(proxyItem);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MakeItem(L.ADD, delegate { AddProfile(); }));
            menu.Items.Add(MakeItem(L.DHCP, delegate { RestoreDhcp(); }));
            menu.Items.Add(MakeItem(L.SETTINGS + "…", delegate { ShowSettings(); }));
            menu.Items.Add(MakeItem(L.ABOUT, delegate { ShowAbout(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MakeItem(L.QUIT, delegate { Quit(); }));
        }

        ToolStripMenuItem MakeItem(string text, Action onClick)
        {
            var it = new ToolStripMenuItem(text);
            it.Click += delegate { onClick(); };
            return it;
        }

        void SwitchTo(Profile p, bool direct)
        {
            Conf.Active = p.Name;
            Conf.Save();
            state = Net.Detect();
            if (!state.Ok)
            {
                MessageBox.Show(string.Format(L.NO_IP_FMT, state.IfName), L.SWITCH_FAIL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Apply.Invoke(state.IfName, false, state.IP, state.Mask,
                direct ? p.DGw : p.OGw, direct ? p.DDns : p.ODns);
            state = Net.Detect();
            tray.Text = TrayText();
        }

        void RestoreDhcp()
        {
            state = Net.Detect();
            if (state.IfName.Length == 0) return;
            Apply.Invoke(state.IfName, true, "", "", "", "");
            state = Net.Detect();
            tray.Text = TrayText();
        }

        void ToggleProxy()
        {
            ProxyCtl.Set(!ProxyCtl.IsOn());
            Rebuild();
        }

        void AddProfile()
        {
            using (var f = new ProfileForm(null))
            {
                if (f.ShowDialog() == DialogResult.OK && f.Result != null)
                {
                    Conf.Profiles.Add(f.Result);
                    Conf.Active = f.Result.Name;
                    Conf.Save();
                }
            }
            Rebuild();
        }

        void EditProfile(Profile p)
        {
            using (var f = new ProfileForm(p))
            {
                if (f.ShowDialog() == DialogResult.OK && f.Result != null)
                {
                    string oldName = p.Name;
                    p.Name = f.Result.Name;
                    p.DGw = f.Result.DGw; p.DDns = f.Result.DDns;
                    p.OGw = f.Result.OGw; p.ODns = f.Result.ODns;
                    if (Conf.Active == oldName) Conf.Active = p.Name;
                    Conf.Save();
                }
            }
            Rebuild();
        }

        void DeleteProfile(Profile p)
        {
            if (Conf.Profiles.Count <= 1)
            {
                MessageBox.Show(L.KEEP_ONE, L.CANT_DELETE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var r = MessageBox.Show(string.Format(L.DEL_Q_FMT, p.Name), L.DLG_TITLE,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (r != DialogResult.OK) return;
            Conf.Profiles.Remove(p);
            if (Conf.Active == p.Name) Conf.Active = Conf.Profiles[0].Name;
            Conf.Save();
            Rebuild();
        }

        void ShowSettings()
        {
            bool check = false;
            using (var f = new SettingsForm())
            {
                f.ShowDialog();
                check = f.CheckRequested;
            }
            Rebuild();
            if (check) ManualUpdateCheck();
        }

        void ManualUpdateCheck()
        {
            ReleaseInfo info = null;
            try
            {
                info = Updater.FetchLatest();
                Conf.LastCheck = DateTime.Now.ToString("yyyyMMdd");
                Conf.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(L.UPDATE_FAIL_FMT, ex.Message), L.UPDATE_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (info == null || info.Version <= Updater.CurrentVersion)
            {
                MessageBox.Show(string.Format(L.UPDATE_NONE, Updater.CurrentText), L.UPDATE_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            RunUpdateFlow(info);
        }

        void RunUpdateFlow(ReleaseInfo info)
        {
            if (info.AssetUrl.Length == 0)
            {
                MessageBox.Show(L.UPDATE_NOASSET, L.UPDATE_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start(info.PageUrl.Length > 0 ? info.PageUrl : Updater.ReleasesPage);
                return;
            }
            var r = MessageBox.Show(
                string.Format(L.UPDATE_FOUND_Q, info.Tag, Updater.CurrentText),
                L.UPDATE_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            try
            {
                Updater.DownloadAndSwap(info.AssetUrl);
                Quit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(L.UPDATE_FAIL_FMT, ex.Message), L.UPDATE_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ShowAbout()
        {
            using (var f = new AboutForm())
            {
                f.ShowDialog();
            }
        }

        void Quit()
        {
            timer.Stop();
            tray.Visible = false;
            tray.Dispose();
            ExitThread();
        }
    }

    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length >= 3 && args[0] == "--apply")
                return Apply.Run(args);

            bool created;
            var mutex = new Mutex(true, "NetworkSwitchTrayApp", out created);
            if (!created) return 0; // already running
            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) { Log.Write("ThreadException: " + e.Exception.GetType().Name + ": " + e.Exception.Message); };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
            GC.KeepAlive(mutex);
            return 0;
        }
    }
}
