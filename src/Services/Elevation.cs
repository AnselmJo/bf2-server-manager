// Admin-Aufträge (eine UAC-Abfrage für mehrere Schritte) - BF2 Manager. C# 5 (.NET Framework 4.x).
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
    // Ein Auftrag ist ein Wörterbuch mit "t" (Typ) und Parametern; mehrere Aufträge laufen mit einer Abfrage.
    public static class Tasks
    {
        static Dictionary<string, object> T(string type) { Dictionary<string, object> d = new Dictionary<string, object>(); d["t"] = type; return d; }
        public static Dictionary<string, object> Setup(string gameDir, string docs, List<string> mods) { Dictionary<string, object> d = T("setup"); d["game"] = gameDir; d["docs"] = docs; d["mods"] = mods; return d; }
        public static Dictionary<string, object> Client(string exe, bool on) { Dictionary<string, object> d = T("client"); d["exe"] = exe; d["on"] = on; return d; }
        public static Dictionary<string, object> Server(string gameDir, string docs, bool on) { Dictionary<string, object> d = T("server"); d["game"] = gameDir; d["docs"] = docs; d["on"] = on; return d; }
        public static Dictionary<string, object> Laa(string exe, bool on) { Dictionary<string, object> d = T("laa"); d["exe"] = exe; d["on"] = on; return d; }
        public static Dictionary<string, object> Firewall(List<string> programs) { Dictionary<string, object> d = T("firewall"); d["programs"] = programs; return d; }
        public static Dictionary<string, object> DirectX(string setup) { Dictionary<string, object> d = T("dx"); d["setup"] = setup; return d; }
        public static Dictionary<string, object> Grant(string dir) { Dictionary<string, object> d = T("grant"); d["dir"] = dir; return d; }
        public static Dictionary<string, object> Swap(string newFile, string target) { Dictionary<string, object> d = T("swap"); d["new"] = newFile; d["target"] = target; return d; }
        public static Dictionary<string, object> Copy(string src, string dst) { Dictionary<string, object> d = T("copy"); d["src"] = src; d["dst"] = dst; return d; }
    }

    public static class Elevation
    {
        public static bool IsAdmin
        {
            get
            {
                try { return new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator); }
                catch { return false; }
            }
        }

        // Führt Aufträge aus; ohne Adminrechte über eine zweite Instanz mit "runas". Ergebniszeilen: "+ ok", "! Fehler".
        public static bool Run(List<Dictionary<string, object>> tasks, out string result)
        {
            if (IsAdmin)
            {
                List<string> log = new List<string>();
                bool ok = Execute(tasks, log);
                result = string.Join("\n", log.ToArray());
                return ok;
            }
            Directory.CreateDirectory(AppPaths.Dir);
            string stamp = DateTime.Now.Ticks.ToString();
            string job = Path.Combine(AppPaths.Dir, "job_" + stamp + ".json"), res = Path.Combine(AppPaths.Dir, "job_" + stamp + ".txt");
            File.WriteAllText(job, new JavaScriptSerializer().Serialize(tasks), new UTF8Encoding(false));
            result = null;
            ProcessStartInfo psi = new ProcessStartInfo(AppPaths.Exe, "--task-job \"" + job + "\" --lang " + (I18n.En ? "en" : "de") + " --result \"" + res + "\"");
            psi.UseShellExecute = true; psi.Verb = "runas";
            try { using (Process p = Process.Start(psi)) { if (p != null) p.WaitForExit(); } }
            catch (Win32Exception) { result = I18n.T("Abgebrochen (keine Adminrechte erteilt).", "Cancelled (administrator rights not granted)."); Cleanup(job, null); return false; }
            try { result = File.Exists(res) ? File.ReadAllText(res, Encoding.UTF8).TrimStart('\uFEFF') : null; } catch { }
            Cleanup(job, res);
            if (result == null) { result = I18n.T("Keine Rückmeldung erhalten.", "No result received."); return false; }
            int nl = result.IndexOf('\n');
            bool okAll = result.StartsWith("OK");
            result = nl >= 0 ? result.Substring(nl + 1) : "";
            return okAll;
        }

        static void Cleanup(string a, string b) { try { if (a != null) File.Delete(a); if (b != null) File.Delete(b); } catch { } }

        public static bool Execute(List<Dictionary<string, object>> tasks, List<string> log)
        {
            bool ok = true;
            foreach (Dictionary<string, object> t in tasks)
            {
                try
                {
                    string type = S(t, "t");
                    if (type == "setup") { List<string> mods = L(t, "mods"); if (!Installer.Run(S(t, "game"), S(t, "docs"), mods, log)) ok = false; }
                    else if (type == "client") log.Add("+ " + ClientUnlock.Apply(S(t, "exe"), B(t, "on")));
                    else if (type == "server") { ServerUnlock.Set(S(t, "game"), S(t, "docs"), B(t, "on")); log.Add("+ " + I18n.T("Server-Unlocks ", "Server unlocks ") + (B(t, "on") ? I18n.T("aktiviert", "enabled") : I18n.T("deaktiviert", "disabled"))); }
                    else if (type == "laa") log.Add("+ " + Pe.SetLaa(S(t, "exe"), B(t, "on")));
                    else if (type == "firewall") foreach (string p in L(t, "programs")) { Firewall.Add(p); log.Add("+ " + I18n.T("Firewall-Freigabe: ", "Firewall rule: ") + Path.GetFileName(p)); }
                    else if (type == "dx")
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(S(t, "setup"), "/silent");
                        psi.UseShellExecute = false; psi.CreateNoWindow = true;
                        using (Process p = Process.Start(psi)) { p.WaitForExit(); if (p.ExitCode != 0) throw new Exception("DXSETUP " + p.ExitCode); }
                        log.Add("+ " + I18n.T("DirectX installiert", "DirectX installed"));
                    }
                    else if (type == "grant") { Installer.Grant(S(t, "dir")); log.Add("+ " + I18n.T("Schreibrechte: ", "Write access: ") + S(t, "dir")); }
                    else if (type == "swap") { Updater.SwapFiles(S(t, "new"), S(t, "target")); log.Add("+ " + I18n.T("Update eingespielt", "Update installed")); }
                    else if (type == "copy") { File.Copy(S(t, "src"), S(t, "dst"), true); log.Add("+ " + I18n.T("Kopiert: ", "Copied: ") + S(t, "dst")); }
                    else throw new Exception("unknown task " + type);
                }
                catch (Exception ex) { ok = false; log.Add("! " + ex.Message); }
            }
            return ok;
        }

        static string S(Dictionary<string, object> d, string k) { object o; return d.TryGetValue(k, out o) && o != null ? o.ToString() : ""; }
        static bool B(Dictionary<string, object> d, string k) { object o; return d.TryGetValue(k, out o) && o is bool && (bool)o; }
        static List<string> L(Dictionary<string, object> d, string k)
        {
            List<string> r = new List<string>(); object o;
            if (d.TryGetValue(k, out o) && o is System.Collections.IEnumerable && !(o is string)) foreach (object x in (System.Collections.IEnumerable)o) r.Add(x.ToString());
            return r;
        }

        // Wird von Program.Main aufgerufen, wenn die exe als Admin-Auftrag gestartet wurde.
        public static bool HandleTask(string[] args)
        {
            string job = null, result = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--result" && i + 1 < args.Length) result = args[++i];
                else if (args[i] == "--lang" && i + 1 < args.Length) I18n.En = args[++i] == "en";
                else if (args[i] == "--task-job" && i + 1 < args.Length) job = args[++i];
            }
            if (job == null) return false;
            List<string> log = new List<string>();
            bool ok = false;
            try
            {
                object[] arr = new JavaScriptSerializer().Deserialize<object[]>(File.ReadAllText(job, Encoding.UTF8));
                List<Dictionary<string, object>> tasks = new List<Dictionary<string, object>>();
                foreach (object o in arr) tasks.Add((Dictionary<string, object>)o);
                ok = Execute(tasks, log);
            }
            catch (Exception ex) { log.Add("! " + ex.Message); }
            if (result != null)
                try { File.WriteAllText(result, (ok ? "OK" : "ERROR") + "\n" + string.Join("\n", log.ToArray()), new UTF8Encoding(false)); } catch { }
            return true;
        }
    }

    // ------------------------------------------------------------------ Windows-Firewall (Programmregeln, eingehend, alle Profile)
    public static class Firewall
    {
        public static string RuleName(string program) { return "BF2 Manager - " + Path.GetFileName(program); }

        static string Netsh(string args, out int code)
        {
            ProcessStartInfo psi = new ProcessStartInfo("netsh.exe", args);
            psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.RedirectStandardOutput = true;
            using (Process p = Process.Start(psi))
            {
                string o = p.StandardOutput.ReadToEnd();
                p.WaitForExit(); code = p.ExitCode; return o;
            }
        }

        // Die Regel zählt nur, wenn sie auf genau diesen Pfad zeigt – nach dem Verschieben der Exe gilt die alte Regel nicht mehr.
        // netsh bricht lange Pfade um; deshalb Leerraum vor dem Vergleich entfernen.
        public static bool Has(string program)
        {
            try
            {
                int code;
                string o = Netsh("advfirewall firewall show rule name=\"" + RuleName(program) + "\" verbose", out code);
                return code == 0 && Squash(o).IndexOf(Squash(Path.GetFullPath(program)), StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        static string Squash(string s) { return System.Text.RegularExpressions.Regex.Replace(s ?? "", @"\s+", ""); }

        public static void Add(string program)
        {
            int code;
            Netsh("advfirewall firewall delete rule name=\"" + RuleName(program) + "\"", out code);
            // Auch die Regeln ersetzen, die Windows bei der ersten Abfrage anlegt (oft nur „Öffentlich“ oder sogar „Blockieren“)
            Netsh("advfirewall firewall delete rule name=all program=\"" + program + "\"", out code);
            string o = Netsh("advfirewall firewall add rule name=\"" + RuleName(program) + "\" dir=in action=allow program=\"" + program + "\" enable=yes profile=any", out code);
            if (code != 0) throw new Exception("netsh: " + o.Trim());
        }
    }
}
