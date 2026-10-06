// Updates über GitHub-Releases oder vom Server-PC im LAN - BF2 Manager. C# 5 (.NET Framework 4.x).
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
    public class ReleaseInfo
    {
        public string Tag, Version, Notes, ExeUrl, SumsUrl, HtmlUrl;
        public bool Pre;
    }

    public static class Updater
    {
        public static string OldPath { get { return AppPaths.Exe + ".old"; } }
        public static string NotesFile { get { return Path.Combine(AppPaths.Dir, "update_notes.txt"); } }

        // Versionsvergleich "1.2.0" / "v1.2.0-beta.2": Vorabversionen sind kleiner als die fertige Version
        public static int Compare(string a, string b)
        {
            string[] pa = Split(a), pb = Split(b);
            string[] ca = pa[0].Split('.'), cb = pb[0].Split('.');
            for (int i = 0; i < Math.Max(ca.Length, cb.Length); i++)
            {
                int x = i < ca.Length ? Util.ToInt(ca[i], 0) : 0, y = i < cb.Length ? Util.ToInt(cb[i], 0) : 0;
                if (x != y) return x.CompareTo(y);
            }
            if (pa[1] == pb[1]) return 0;
            if (pa[1].Length == 0) return 1;
            if (pb[1].Length == 0) return -1;
            string[] qa = pa[1].Split('.'), qb = pb[1].Split('.');
            for (int i = 0; i < Math.Max(qa.Length, qb.Length); i++)
            {
                if (i >= qa.Length) return -1;
                if (i >= qb.Length) return 1;
                int na, nb;
                bool ia = int.TryParse(qa[i], out na), ib = int.TryParse(qb[i], out nb);
                int c = ia && ib ? na.CompareTo(nb) : string.CompareOrdinal(qa[i], qb[i]);
                if (c != 0) return c;
            }
            return 0;
        }

        static string[] Split(string v)
        {
            v = (v ?? "0").Trim().TrimStart('v', 'V');
            int i = v.IndexOf('-');
            return i < 0 ? new string[] { v, "" } : new string[] { v.Substring(0, i), v.Substring(i + 1) };
        }

        static HttpWebRequest Req(string url, int timeout)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);
            r.UserAgent = "BF2Manager/" + AppInfo.Version;
            r.Timeout = timeout; r.ReadWriteTimeout = timeout;
            r.Accept = "application/vnd.github+json, */*";
            return r;
        }

        public static string GetText(string url, int timeout)
        {
            using (WebResponse resp = Req(url, timeout).GetResponse())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) return sr.ReadToEnd();
        }

        // Neueste passende Version auf GitHub, die neuer ist als die laufende; sonst null
        public static ReleaseInfo Latest(bool pre, int timeout)
        {
            string json = GetText("https://api.github.com/repos/" + AppInfo.Repo + "/releases?per_page=15", timeout);
            object[] arr = new JavaScriptSerializer().DeserializeObject(json) as object[];
            ReleaseInfo best = null;
            if (arr == null) return null;
            foreach (object o in arr)
            {
                Dictionary<string, object> d = o as Dictionary<string, object>;
                if (d == null || B(d, "draft")) continue;
                bool isPre = B(d, "prerelease");
                if (isPre && !pre) continue;
                ReleaseInfo r = new ReleaseInfo();
                r.Tag = S(d, "tag_name"); r.Version = r.Tag.TrimStart('v', 'V'); r.Notes = S(d, "body"); r.HtmlUrl = S(d, "html_url"); r.Pre = isPre;
                object[] assets = d.ContainsKey("assets") ? d["assets"] as object[] : null;
                if (assets != null)
                    foreach (object ao in assets)
                    {
                        Dictionary<string, object> a = ao as Dictionary<string, object>;
                        if (a == null) continue;
                        string name = S(a, "name"), url = S(a, "browser_download_url");
                        if (name.Equals(AppInfo.ExeName, StringComparison.OrdinalIgnoreCase)) r.ExeUrl = url;
                        else if (name.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase)) r.SumsUrl = url;
                    }
                if (r.ExeUrl == null || r.SumsUrl == null) continue;
                if (Compare(r.Version, AppInfo.Version) <= 0) continue;
                if (best == null || Compare(r.Version, best.Version) > 0) best = r;
            }
            return best;
        }

        static string S(Dictionary<string, object> d, string k) { object o; return d.TryGetValue(k, out o) && o != null ? o.ToString() : ""; }
        static bool B(Dictionary<string, object> d, string k) { object o; return d.TryGetValue(k, out o) && o is bool && (bool)o; }

        public static string Sha256(string file)
        {
            using (SHA256 h = SHA256.Create())
            using (FileStream fs = File.OpenRead(file)) return Hex(h.ComputeHash(fs));
        }
        public static string Hex(byte[] b) { StringBuilder sb = new StringBuilder(); foreach (byte x in b) sb.Append(x.ToString("x2")); return sb.ToString(); }

        // Erwarteter Hash aus SHA256SUMS ("<hash>  BF2Manager.exe")
        public static string ExpectedHash(string sums)
        {
            foreach (string l in sums.Replace("\r", "").Split('\n'))
            {
                string[] p = l.Trim().Split(new char[] { ' ', '\t', '*' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length >= 2 && p[1].Equals(AppInfo.ExeName, StringComparison.OrdinalIgnoreCase) && p[0].Length == 64) return p[0].ToLowerInvariant();
                if (p.Length == 1 && p[0].Length == 64) return p[0].ToLowerInvariant();
            }
            return null;
        }

        // Lädt die neue exe herunter und prüft den Hash; Rückgabe: Pfad der geprüften Datei
        public static string Download(string exeUrl, string sumsUrl, Action<long, long> progress, int timeout)
        {
            string expected = ExpectedHash(GetText(sumsUrl, timeout));
            if (expected == null) throw new Exception(I18n.T("Prüfsumme fehlt im Release.", "Checksum missing in release."));
            string dir = Installer.CanWrite(AppPaths.ExeDir) ? AppPaths.ExeDir : AppPaths.Dir;
            Directory.CreateDirectory(dir);
            string tmp = Path.Combine(dir, AppInfo.ExeName + ".new");
            using (WebResponse resp = Req(exeUrl, timeout).GetResponse())
            using (Stream src = resp.GetResponseStream())
            using (FileStream dst = new FileStream(tmp, FileMode.Create, FileAccess.Write))
            {
                long total = resp.ContentLength, done = 0; byte[] buf = new byte[65536]; int n;
                while ((n = src.Read(buf, 0, buf.Length)) > 0) { dst.Write(buf, 0, n); done += n; if (progress != null) progress(done, total); }
            }
            string got = Sha256(tmp);
            if (got != expected) { try { File.Delete(tmp); } catch { } throw new Exception(I18n.T("Prüfsumme stimmt nicht – Update verworfen.", "Checksum mismatch – update discarded.")); }
            return tmp;
        }

        // Laufende exe in .old umbenennen und neue Datei an ihre Stelle setzen (Windows erlaubt das Umbenennen laufender exe)
        public static void SwapFiles(string newFile, string target)
        {
            string old = target + ".old";
            if (File.Exists(old)) File.Delete(old);
            File.Move(target, old);
            try { File.Move(newFile, target); }
            catch { File.Move(old, target); throw; }
        }

        // Spielt die geprüfte Datei ein; falls nötig mit Adminrechten. Danach muss neu gestartet werden.
        public static void Install(string newFile)
        {
            if (Installer.CanWrite(AppPaths.ExeDir)) { SwapFiles(newFile, AppPaths.Exe); return; }
            List<Dictionary<string, object>> t = new List<Dictionary<string, object>>();
            t.Add(Tasks.Swap(newFile, AppPaths.Exe));
            string res;
            if (!Elevation.Run(t, out res)) throw new Exception(res);
        }

        public static void Restart(string fromVersion)
        {
            ProcessStartInfo psi = new ProcessStartInfo(AppPaths.Exe, "--updated " + fromVersion);
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        public static bool CanRollback { get { return File.Exists(OldPath); } }

        // Vorherige Version wiederherstellen (aus .old)
        public static void Rollback()
        {
            string bad = AppPaths.Exe + ".bad";
            if (File.Exists(bad)) File.Delete(bad);
            File.Move(AppPaths.Exe, bad);
            File.Move(OldPath, AppPaths.Exe);
        }

        public static void CleanupAfterStart()
        {
            try { string bad = AppPaths.Exe + ".bad"; if (File.Exists(bad)) File.Delete(bad); } catch { }
            try { string n = Path.Combine(AppPaths.ExeDir, AppInfo.ExeName + ".new"); if (File.Exists(n)) File.Delete(n); } catch { }
        }
    }
}
