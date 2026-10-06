// Diagnose-Paket für Fehlermeldungen - BF2 Manager. C# 5 (.NET Framework 4.x).
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
    public static class Diagnostics
    {
        // ZIP mit Konfiguration, Logs, Versionen und PC-Check – ohne Spieldateien und ohne Passwörter
        public static void Export(string zipPath, string gameDir, IEnumerable<string> mods)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            using (ZipArchive z = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(AppInfo.Name + " " + AppInfo.Version);
                sb.AppendLine("OS: " + Environment.OSVersion + (Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit") + ", CLR " + Environment.Version);
                sb.AppendLine("Exe: " + AppPaths.Exe);
                sb.AppendLine("Game: " + gameDir + "  BF2.exe " + GameLocator.FileVersion(Path.Combine(gameDir ?? "", "BF2.exe")));
                sb.AppendLine("Portable: " + (GameLocator.Portable() != null));
                foreach (string m in mods)
                {
                    ModInfo mi = ModInfo.Read(gameDir, m);
                    ModStatus st = Installer.Check(gameDir, m);
                    sb.AppendLine("Mod " + m + ": " + mi.Display + "  module=" + st.Version + " hooked=" + st.Hooked + " writable=" + st.Writable);
                }
                sb.AppendLine();
                try { foreach (CheckItem c in PcCheck.Run(gameDir, AppPaths.Docs)) sb.AppendLine("[" + c.Status + "] " + c.Title + ": " + c.Detail); } catch (Exception ex) { sb.AppendLine("PcCheck failed: " + ex.Message); }
                Add(z, "info.txt", sb.ToString());
                AddFile(z, AppPaths.Config, "config.json");
                AddFile(z, AppPaths.Log, "manager_error.log");
                foreach (string m in mods)
                    foreach (string f in new string[] { "coop_status.txt", "coop_server.txt", "coop_live.txt", "coop_rounds.txt" })
                        AddFile(z, Path.Combine(gameDir, "mods", m, "settings", f), "mods/" + m + "/" + f);
            }
        }

        static void Add(ZipArchive z, string name, string text)
        {
            using (StreamWriter w = new StreamWriter(z.CreateEntry(name).Open(), new UTF8Encoding(false))) w.Write(Scrub(text));
        }

        // Entfernt persönliche Daten: Benutzerordner/-name, Passwörter, öffentliche IP-Adressen (private LAN-Adressen bleiben)
        public static string Scrub(string t)
        {
            if (string.IsNullOrEmpty(t)) return t;
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home) && home.Length > 3) { t = t.Replace(home, "%USERPROFILE%"); t = t.Replace(home.Replace('\\', '/'), "%USERPROFILE%"); }
            string user = Environment.UserName;
            if (!string.IsNullOrEmpty(user) && user.Length >= 3) t = Regex.Replace(t, "(?<![A-Za-z0-9])" + Regex.Escape(user) + "(?![A-Za-z0-9])", "<user>", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, "(?i)(sv\\.password\\s+)\"[^\"]*\"", "$1\"***\"");
            t = Regex.Replace(t, "(?i)(\"[A-Za-z.]*password\"\\s*:\\s*\")[^\"]*\"", "$1***\"");
            t = Regex.Replace(t, "\\b(\\d{1,3})\\.(\\d{1,3})\\.(\\d{1,3})\\.(\\d{1,3})\\b", delegate (Match m)
            {
                int a = int.Parse(m.Groups[1].Value), b = int.Parse(m.Groups[2].Value);
                bool priv = a == 10 || a == 127 || (a == 192 && b == 168) || (a == 172 && b >= 16 && b <= 31) || (a == 169 && b == 254) || a == 0 || a == 255 || a == 1;
                return priv ? m.Value : "x.x.x.x";
            });
            return t;
        }

        static void AddFile(ZipArchive z, string path, string name)
        {
            try
            {
                if (!File.Exists(path)) return;
                string t = File.ReadAllText(path);
                if (t.Length > 400000) t = t.Substring(t.Length - 400000);
                Add(z, name, t);
            }
            catch { }
        }
    }
}
