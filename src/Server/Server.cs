// Serverprozess, Live-Kanal, Log - BF2 Manager. C# 5 (.NET Framework 4.x).
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using WinTimer = System.Windows.Forms.Timer;
using Timer = System.Windows.Forms.Timer;

namespace BF2Coop
{
    // ------------------------------------------------------------------ Server, Live-Kanal, Netzwerk
    public static class Srv
    {
        public static bool FakeRunning;
        public static bool IsRunning() { return FakeRunning || Count("bf2_w32ded") > 0; }
        public static bool GameRunning() { return Count("BF2") > 0; }

        static int Count(string name)
        {
            try
            {
                Process[] ps = Process.GetProcessesByName(name);
                int n = ps.Length;
                foreach (Process p in ps) p.Dispose();
                return n;
            }
            catch { return 0; }
        }

        public static void Kill()
        {
            foreach (Process p in Process.GetProcessesByName("bf2_w32ded"))
            {
                try { p.Kill(); p.WaitForExit(3000); } catch { }
                p.Dispose();
            }
        }

        public static string Args(string mod, bool coop)
        {
            string folder = coop ? "coop" : "conquest";
            return (coop ? "+ai 1 +gameMode gamemode " : "") +
                "+config \"@HOME@/ServerConfigs/" + folder + "/serverSettings.con\" +mapList \"@HOME@/ServerConfigs/" + folder + "/maplist.con\" +modPath mods/" + mod + " +ignoreAsserts 1";
        }

        public static List<string> LanAddresses()
        {
            List<string> ips = new List<string>();
            try
            {
                foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (n.OperationalStatus != OperationalStatus.Up || n.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (UnicastIPAddressInformation a in n.GetIPProperties().UnicastAddresses)
                    {
                        string s = a.Address.ToString();
                        if (a.Address.AddressFamily == AddressFamily.InterNetwork && !s.StartsWith("169.254")) ips.Add(s);
                    }
                }
            }
            catch { }
            return ips;
        }
    }

    public static class CmdChannel
    {
        static long _last;

        // Kleine, streng steigende IDs (passen in einen int von Python 2.3)
        public static void Send(string settingsDir, string cmd)
        {
            long id = (long)(DateTime.UtcNow - new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds * 10;
            if (id <= _last) id = _last + 1;
            _last = id;
            File.AppendAllText(Path.Combine(settingsDir, "coop_cmd.txt"), id + " " + cmd + "\r\n", Encoding.ASCII);
        }
    }

    public class LogTail
    {
        long _len = -1; DateTime _time;

        public string[] ReadIfChanged(string path, int maxLines)
        {
            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists) { if (_len == 0) return null; _len = 0; return new string[0]; }
                if (fi.Length == _len && fi.LastWriteTimeUtc == _time) return null;
                _len = fi.Length; _time = fi.LastWriteTimeUtc;
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long start = Math.Max(0, fs.Length - 64 * 1024);
                    fs.Seek(start, SeekOrigin.Begin);
                    byte[] buf = new byte[fs.Length - start];
                    int read = 0;
                    while (read < buf.Length) { int n = fs.Read(buf, read, buf.Length - read); if (n <= 0) break; read += n; }
                    string[] lines = Util.Ansi.GetString(buf, 0, read).Replace("\r", "").Split('\n');
                    List<string> l = new List<string>();
                    foreach (string s in lines) if (s.Length > 0) l.Add(s);
                    if (start > 0 && l.Count > 0) l.RemoveAt(0);
                    if (l.Count > maxLines) l.RemoveRange(0, l.Count - maxLines);
                    return l.ToArray();
                }
            }
            catch { return null; }
        }
    }
}
