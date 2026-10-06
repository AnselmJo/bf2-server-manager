// Spielordner, Versionen, Mods, Ablageorte - BF2 Manager. C# 5 (.NET Framework 4.x).
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

[assembly: AssemblyTitle("BF2 Manager")]
[assembly: AssemblyProduct("BF2 Manager")]
[assembly: AssemblyDescription("Launcher and server manager for Battlefield 2 LAN parties with bots")]
[assembly: AssemblyVersion("1.2.1.0")]
[assembly: AssemblyFileVersion("1.2.1.0")]

namespace BF2Coop
{
    public static class AppInfo
    {
        public const string Name = "BF2 Manager";
        public const string Version = "1.2.1";
        public const int ModuleVersion = 4;
        public const string Repo = "AnselmJo/bf2-manager";
        public const string RepoUrl = "https://github.com/" + Repo;
        public const string ExeName = "BF2Manager.exe";
    }

    // ------------------------------------------------------------------ Ablageorte
    // Persönliches liegt pro Windows-Benutzer in %APPDATA%; gemeinsame LAN-Dateien im Ordner _LAN neben BF2.exe.
    public static class AppPaths
    {
        public static string Dir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BF2Manager"); } }
        public static string Config { get { return Path.Combine(Dir, "config.json"); } }
        public static string Cache { get { return Path.Combine(Dir, "cache"); } }
        public static string Presets { get { return Path.Combine(Dir, "presets"); } }
        public static string Log { get { return Path.Combine(Dir, "manager_error.log"); } }
        public static string Exe { get { return Assembly.GetExecutingAssembly().Location; } }
        public static string ExeDir { get { return Path.GetDirectoryName(Exe) ?? "."; } }
        public static string Docs { get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); } }
        public static string LanDir(string gameDir) { return Path.Combine(gameDir, "_LAN"); }

        // Übernimmt Einstellungen älterer Versionen (BF2 Server Manager, BF2 Coop Manager), falls vorhanden.
        public static void Migrate()
        {
            if (File.Exists(Config)) return;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string[] olds = { Path.Combine(appData, "BF2ServerManager", "config.json"), Path.Combine(ExeDir, "config.json"), Path.Combine(Docs, "BF2CoopManager", "config.json") };
            foreach (string o in olds)
            {
                try
                {
                    if (!File.Exists(o)) continue;
                    Directory.CreateDirectory(Dir);
                    File.Copy(o, Config);
                    string pd = Path.Combine(Path.GetDirectoryName(o), "presets");
                    if (Directory.Exists(pd))
                    {
                        Directory.CreateDirectory(Presets);
                        foreach (string f in Directory.GetFiles(pd, "*.json")) File.Copy(f, Path.Combine(Presets, Path.GetFileName(f)), true);
                    }
                    return;
                }
                catch { }
            }
        }
    }

    // ------------------------------------------------------------------ Spielordner finden
    public static class GameLocator
    {
        public static bool IsGameDir(string d)
        {
            return !string.IsNullOrEmpty(d) && (File.Exists(Path.Combine(d, "BF2.exe")) || File.Exists(Path.Combine(d, "bf2_w32ded.exe")));
        }

        // Liegt der Manager im Spielordner (LAN-Ordner), gilt dieser immer.
        public static string Portable() { return IsGameDir(AppPaths.ExeDir) ? AppPaths.ExeDir.TrimEnd('\\', '/') : null; }

        public static string Find()
        {
            string p = Portable();
            if (p != null) return p;
            List<string> cands = new List<string>();
            foreach (RegistryView v in new RegistryView[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using (RegistryKey b = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, v))
                    using (RegistryKey k = b.OpenSubKey(@"SOFTWARE\Electronic Arts\EA Games\Battlefield 2"))
                        if (k != null) { object o = k.GetValue("InstallDir"); if (o != null) cands.Add(o.ToString()); }
                }
                catch { }
            }
            string pf86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)"), pf = Environment.GetEnvironmentVariable("ProgramFiles");
            if (pf86 != null) cands.Add(Path.Combine(pf86, "EA GAMES", "Battlefield 2"));
            if (pf != null) cands.Add(Path.Combine(pf, "EA GAMES", "Battlefield 2"));
            foreach (string c in cands) if (IsGameDir(c)) return c.TrimEnd('\\', '/');
            return null;
        }

        // Mods, in die das Bot-Modul passt (python\game\scoringCommon.py vorhanden)
        public static List<string> Mods(string gameDir)
        {
            List<string> r = new List<string>();
            try
            {
                foreach (string d in Directory.GetDirectories(Path.Combine(gameDir, "mods")))
                    if (File.Exists(Path.Combine(d, "python", "game", "scoringCommon.py"))) r.Add(Path.GetFileName(d));
            }
            catch { }
            r.Sort(StringComparer.OrdinalIgnoreCase);
            return r;
        }

        // Alle Mod-Ordner (auch ohne Python), z. B. für Spieler-Seite und LAN-Abgleich
        public static List<string> AllMods(string gameDir)
        {
            List<string> r = new List<string>();
            try { foreach (string d in Directory.GetDirectories(Path.Combine(gameDir, "mods"))) if (File.Exists(Path.Combine(d, "mod.desc")) || Directory.Exists(Path.Combine(d, "levels"))) r.Add(Path.GetFileName(d)); }
            catch { }
            r.Sort(StringComparer.OrdinalIgnoreCase);
            return r;
        }

        public static string FileVersion(string exe)
        {
            try
            {
                if (!File.Exists(exe)) return null;
                FileVersionInfo v = FileVersionInfo.GetVersionInfo(exe);
                string s = !string.IsNullOrEmpty(v.ProductVersion) ? v.ProductVersion : v.FileVersion;
                return string.IsNullOrEmpty(s) ? "?" : s.Replace(", ", ".").Trim();
            }
            catch { return null; }
        }
    }

    // mod.desc: <title>, <version>, <description>
    public class ModInfo
    {
        public string Name, Title, Version, Description;
        public static ModInfo Read(string gameDir, string mod)
        {
            ModInfo m = new ModInfo(); m.Name = mod; m.Title = mod;
            try
            {
                string p = Path.Combine(gameDir, "mods", mod, "mod.desc");
                if (!File.Exists(p)) return m;
                string t = File.ReadAllText(p);
                m.Title = Tag(t, "title") ?? mod;
                m.Version = Tag(t, "version");
                m.Description = Tag(t, "description");
            }
            catch { }
            return m;
        }
        static string Tag(string xml, string tag)
        {
            Match x = Regex.Match(xml, "<" + tag + @"\b[^>]*>(.*?)</" + tag + ">", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!x.Success) return null;
            string v = Regex.Replace(x.Groups[1].Value, @"\s+", " ").Trim();
            return v.Length == 0 ? null : v;
        }
        public string Display { get { return Title + (string.IsNullOrEmpty(Version) ? "" : " " + Version); } }
    }

    // ------------------------------------------------------------------ BF2-Profile in Dokumente\Battlefield 2\Profiles
    public class Bf2Profile
    {
        public string Id, Dir, Name, Nick, GamespyNick;
        public bool Online { get { return !string.IsNullOrEmpty(GamespyNick); } }
        public string Display { get { return (string.IsNullOrEmpty(Nick) ? Name : Nick) + "  (" + Id + (Online ? ", online" : "") + ")"; } }
    }

    public static class Profiles
    {
        public static string Root { get { return Path.Combine(AppPaths.Docs, "Battlefield 2", "Profiles"); } }
        static readonly Regex RxSet = new Regex(@"^\s*(\w+)\.(\w+)\s+(.*?)\s*$");

        public static List<Bf2Profile> All()
        {
            List<Bf2Profile> r = new List<Bf2Profile>();
            try
            {
                foreach (string d in Directory.GetDirectories(Root))
                {
                    string id = Path.GetFileName(d);
                    if (!Regex.IsMatch(id, @"^\d{4}$")) continue;
                    string pc = Path.Combine(d, "Profile.con");
                    if (!File.Exists(pc)) continue;
                    Bf2Profile p = new Bf2Profile(); p.Id = id; p.Dir = d;
                    p.Name = Get(pc, "LocalProfile", "setName"); p.Nick = Get(pc, "LocalProfile", "setNick"); p.GamespyNick = Get(pc, "LocalProfile", "setGamespyNick");
                    r.Add(p);
                }
            }
            catch { }
            return r;
        }

        public static string DefaultId()
        {
            return Get(Path.Combine(Root, "Global.con"), "GlobalSettings", "setDefaultUser");
        }

        public static Bf2Profile Default()
        {
            List<Bf2Profile> all = All();
            string id = DefaultId();
            foreach (Bf2Profile p in all) if (p.Id == id) return p;
            return all.Count > 0 ? all[0] : null;
        }

        // Liest den Wert einer Zeile "Objekt.setX Wert" (ohne Anführungszeichen)
        public static string Get(string file, string obj, string key)
        {
            foreach (string l in Util.ReadLines(file))
            {
                Match m = RxSet.Match(l);
                if (m.Success && m.Groups[1].Value.Equals(obj, StringComparison.OrdinalIgnoreCase) && m.Groups[2].Value.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return m.Groups[3].Value.Trim().Trim('"');
            }
            return null;
        }

        // Setzt bzw. ergänzt Zeilen "Objekt.setX Wert"; Rest der Datei bleibt unverändert
        public static void Set(string file, string obj, IDictionary<string, string> values)
        {
            List<string> lines = new List<string>(Util.ReadLines(file));
            HashSet<string> done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < lines.Count; i++)
            {
                Match m = RxSet.Match(lines[i]);
                string v;
                if (m.Success && m.Groups[1].Value.Equals(obj, StringComparison.OrdinalIgnoreCase) && values.TryGetValue(m.Groups[2].Value, out v))
                { lines[i] = obj + "." + m.Groups[2].Value + " " + v; done.Add(m.Groups[2].Value); }
            }
            foreach (KeyValuePair<string, string> kv in values) if (!done.Contains(kv.Key)) lines.Add(obj + "." + kv.Key + " " + kv.Value);
            Util.WriteAtomic(file, string.Join("\r\n", lines.ToArray()) + "\r\n", Util.Ansi);
        }

        public static void SetDefault(string id)
        {
            string g = Path.Combine(Root, "Global.con");
            if (!File.Exists(g)) return;
            Dictionary<string, string> v = new Dictionary<string, string>(); v["setDefaultUser"] = "\"" + id + "\"";
            Set(g, "GlobalSettings", v);
        }

        public static string CleanName(string n)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in (n ?? "").Trim()) if (c > 32 && c < 127 && c != '"' && c != '|') sb.Append(c); else if (c == ' ') sb.Append('_');
            string s = sb.ToString();
            return s.Length > 24 ? s.Substring(0, 24) : s;
        }

        public static string CacheDir(string mod) { return Path.Combine(AppPaths.Docs, "Battlefield 2", "mods", mod, "cache"); }
    }
}
