// Konfiguration und Hilfsfunktionen - BF2 Manager. C# 5 (.NET Framework 4.x).
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
    // ------------------------------------------------------------------ Konfiguration und Presets (JSON)
    public class AppConfig
    {
        public Dictionary<string, object> Raw = new Dictionary<string, object>();
        string _path;

        public static AppConfig Empty(string path) { AppConfig c = new AppConfig(); c._path = path; return c; }

        public static AppConfig Load(string path)
        {
            AppConfig c = new AppConfig();
            c._path = path;
            Dictionary<string, object> d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(path, Encoding.UTF8));
            if (d != null) c.Raw = d;
            return c;
        }

        public string S(string k, string def) { object o; return Raw.TryGetValue(k, out o) && o != null ? Convert.ToString(o, CultureInfo.InvariantCulture) : def; }
        public int I(string k, int def) { return Util.ToInt(S(k, null), def); }
        public bool B(string k, bool def) { string s = S(k, null); return s == null ? def : (s == "1" || s.ToLowerInvariant() == "true"); }
        public void Set(string k, object v) { Raw[k] = v; }

        public List<string> List(string k)
        {
            List<string> r = new List<string>();
            object o;
            if (!Raw.TryGetValue(k, out o) || o == null) return r;
            if (o is string) { r.Add((string)o); return r; }
            IEnumerable e = o as IEnumerable;
            if (e != null) foreach (object x in e) if (x != null) r.Add(x.ToString());
            return r;
        }

        public void Save()
        {
            try { Util.WriteAtomic(_path, new JavaScriptSerializer().Serialize(Raw), new UTF8Encoding(false)); } catch { }
        }
    }

    // ------------------------------------------------------------------ Hilfsfunktionen
    public static class Util
    {
        public static readonly Encoding Ansi = Encoding.Default;

        public static int ToInt(string s, int def) { int n; return int.TryParse((s ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : def; }
        public static int Clamp(int v, int lo, int hi) { if (hi < lo) return v; return v < lo ? lo : (v > hi ? hi : v); }

        public static string[] Skip(string[] a, int n)
        {
            if (a.Length <= n) return new string[0];
            string[] r = new string[a.Length - n];
            Array.Copy(a, n, r, 0, r.Length);
            return r;
        }

        public static string[] ReadLines(string path)
        {
            try { return File.Exists(path) ? File.ReadAllLines(path, Ansi) : new string[0]; } catch { return new string[0]; }
        }

        public static void WriteAtomic(string path, string text, Encoding enc)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text, enc);
            if (File.Exists(path))
            {
                try { File.Replace(tmp, path, null); return; }
                catch (UnauthorizedAccessException) { File.Delete(tmp); throw; }
                catch { File.Copy(tmp, path, true); File.Delete(tmp); return; }
            }
            File.Move(tmp, path);
        }

        public static string SafeName(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            return sb.ToString();
        }

        static readonly Regex Small = new Regex(@"(?<=\s)(Of|At|The|On|In|To)\b");
        public static string Pretty(string name)
        {
            string t = CultureInfo.InvariantCulture.TextInfo.ToTitleCase((name ?? "").Replace('_', ' ').ToLowerInvariant());
            return Small.Replace(t, delegate (Match m) { return m.Value.ToLowerInvariant(); });
        }

        public static string Backup(IEnumerable<string> paths, string docsDir)
        {
            string root = Path.Combine(docsDir, "BF2_Backups");
            string dir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            int n = 0;
            foreach (string p in paths)
            {
                if (!File.Exists(p)) continue;
                Directory.CreateDirectory(dir);
                string leaf = Path.GetFileName(Path.GetDirectoryName(p)) + "_" + Path.GetFileName(p);
                File.Copy(p, Path.Combine(dir, leaf), true);
                n++;
            }
            try
            {
                if (Directory.Exists(root))
                {
                    string[] all = Directory.GetDirectories(root);
                    Array.Sort(all, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < all.Length - 40; i++) Directory.Delete(all[i], true);
                }
            }
            catch { }
            return n > 0 ? dir : null;
        }

        public static void Log(string appDir, string text)
        {
            try { File.AppendAllText(Path.Combine(appDir, "manager_error.log"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text + "\r\n\r\n", Encoding.UTF8); } catch { }
        }
    }
}
