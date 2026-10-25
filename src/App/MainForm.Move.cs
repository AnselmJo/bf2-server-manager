// Hauptfenster: Manager in den Spielordner kopieren (feste Firewall-Freigabe, Desktop-Verknüpfung) - BF2 Manager. C# 5.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BF2Coop
{
    public partial class MainForm
    {
        // Standard-Anordnung: Manager neben dem Spielordner (Battlefield2\BF2Manager.exe + Battlefield2\Battlefield 2\BF2.exe).
        // Liegt das Spiel in einem Systemordner (z. B. C:\Program Files (x86)\EA GAMES), kommt der Manager in den Spielordner selbst.
        string MoveDir
        {
            get
            {
                try
                {
                    string parent = Path.GetDirectoryName(GameDir.TrimEnd('\\', '/'));
                    if (string.IsNullOrEmpty(parent) || parent.Length <= 3) return GameDir;
                    string p = parent.TrimEnd('\\', '/') + "\\";
                    foreach (string sys in new string[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)"), Environment.GetEnvironmentVariable("windir") })
                        if (!string.IsNullOrEmpty(sys) && p.StartsWith(sys.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return GameDir;
                    return parent;
                }
                catch { return GameDir; }
            }
        }

        bool MoveIsBeside { get { return !string.Equals(MoveDir.TrimEnd('\\', '/'), GameDir.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase); } }
        string GameDirExe { get { return Path.Combine(MoveDir, AppInfo.ExeName); } }

        // Angebot nur, wenn das Spiel gefunden ist und der Manager woanders liegt (z. B. Downloads)
        bool CanMoveToGameDir
        {
            get { return _shotDir == null && GameLocator.Portable() == null && GameLocator.IsGameDir(GameDir); }
        }

        // Einmal fragen, sobald der Spielordner bekannt ist
        void OfferMoveOnce()
        {
            if (!CanMoveToGameDir || _cfg.B("MoveAsked", false)) return;
            _cfg.Set("MoveAsked", true); _cfg.Save();
            bool beside = MoveIsBeside;
            int r = DarkDialog.Ask(this, beside ? T("Manager neben den Spielordner legen?", "Put the manager next to the game folder?") : T("Manager in den Spielordner legen?", "Put the manager into the game folder?"),
                T("Der Manager läuft gerade aus\n", "The manager is currently running from\n") + AppPaths.ExeDir + "\n\n" + T("Besser aufgehoben wäre er hier:\n", "A better place would be:\n") + MoveDir + "\n\n" +
                T("• Die Firewall-Freigabe bleibt gültig – Server im LAN werden zuverlässig gefunden.\n• Der Spielordner wird automatisch gefunden" + (beside ? " (der Manager liegt daneben)" : "") + ".\n• Kopierst du den Ordner für eine LAN-Party, ist der Manager gleich dabei.\n• Eine Verknüpfung „BF2 Manager“ kommt auf den Desktop.\n\nEinstellungen und Profile bleiben, wie sie sind. Jederzeit auch unter Einstellungen › Ordner.",
                  "• The firewall rule stays valid – servers on the LAN are found reliably.\n• The game folder is found automatically" + (beside ? " (the manager sits next to it)" : "") + ".\n• When you copy the folder for a LAN party, the manager comes along.\n• A “BF2 Manager” shortcut is put on the desktop.\n\nSettings and profiles stay as they are. Also available any time under Settings › Folders."),
                1, false, T("Nicht jetzt", "Not now"), beside ? T("Neben den Spielordner kopieren", "Copy next to the game folder") : T("In den Spielordner kopieren", "Copy to game folder"));
            if (r == 1) MoveToGameDir();
        }

        void MoveToGameDir()
        {
            if (!CanMoveToGameDir) return;
            string target = GameDirExe;
            if (_running) { Toast(T("Bitte zuerst den Server beenden.", "Please stop the server first."), ToastKind.Warn); return; }
            // Liegt dort schon eine neuere Version, einfach diese starten
            string there = GameLocator.FileVersion(target);
            if (there != null && there != "?" && Updater.Compare(there, AppInfo.Version) > 0) { StartMoved(target); return; }
            List<Dictionary<string, object>> tasks = new List<Dictionary<string, object>>();
            try { File.Copy(AppPaths.Exe, target, true); }
            catch (UnauthorizedAccessException) { tasks.Add(Tasks.Copy(AppPaths.Exe, target)); }
            catch (IOException ex) { Toast(T("Kopieren fehlgeschlagen: ", "Copy failed: ") + ex.Message, ToastKind.Error); return; }
            tasks.Add(Tasks.Firewall(new List<string> { target }));
            Toast(T("Bitte die Windows-Abfrage bestätigen (Firewall-Freigabe).", "Please confirm the Windows prompt (firewall rule)."), ToastKind.Info);
            RunTasks(tasks, delegate (bool ok, string result)
            {
                if (!File.Exists(target)) { Toast(T("Nicht kopiert: ", "Not copied: ") + FormatLog(result), ToastKind.Error); return; }
                if (!ok) Toast(FormatLog(result), ToastKind.Warn);
                CreateDesktopShortcut(target);
                StartMoved(target);
            });
        }

        void StartMoved(string target)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(target, "--moved \"" + AppPaths.Exe + "\"");
                psi.UseShellExecute = false; psi.WorkingDirectory = Path.GetDirectoryName(target);
                Process.Start(psi);
                _updExit = true; Close();
            }
            catch (Exception ex) { Toast(T("Start fehlgeschlagen: ", "Start failed: ") + ex.Message, ToastKind.Error); }
        }

        // Verknüpfung über WScript.Shell (spät gebunden, keine Zusatz-DLL)
        static void CreateDesktopShortcut(string target)
        {
            CreateShortcut(target, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "BF2 Manager.lnk"));
        }

        public static void CreateShortcut(string target, string path)
        {
            object sh = null, lnk = null;
            try
            {
                Type t = Type.GetTypeFromProgID("WScript.Shell");
                if (t == null) return;
                sh = Activator.CreateInstance(t);
                lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { path });
                Type lt = lnk.GetType();
                lt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
                lt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { Path.GetDirectoryName(target) });
                lt.InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { AppInfo.Name });
                lt.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { target + ",0" });
                lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
            }
            catch { }
            finally
            {
                if (lnk != null && Marshal.IsComObject(lnk)) Marshal.ReleaseComObject(lnk);
                if (sh != null && Marshal.IsComObject(sh)) Marshal.ReleaseComObject(sh);
            }
        }

        // Nach dem Umzug: kurz bestätigen und anbieten, die alte Kopie zu löschen
        void AfterMove()
        {
            string old = Program.MovedFrom;
            if (old == null || _shotDir != null) return;
            if (string.Equals(Path.GetFullPath(old), AppPaths.Exe, StringComparison.OrdinalIgnoreCase) || !File.Exists(old)) { Toast(T("Der Manager läuft jetzt aus dem Spielordner.", "The manager now runs from the game folder."), ToastKind.Ok); return; }
            int r = DarkDialog.Ask(this, T("Umzug fertig", "Move complete"),
                T("Der Manager läuft jetzt aus dem Spielordner, die Verknüpfung „BF2 Manager“ liegt auf dem Desktop.\n\nDie alte Datei wird nicht mehr gebraucht:\n", "The manager now runs from the game folder, the “BF2 Manager” shortcut is on the desktop.\n\nThe old file is no longer needed:\n") + old,
                0, false, T("Behalten", "Keep"), T("Alte Datei löschen", "Delete old file"));
            if (r != 1) return;
            // Die alte Instanz braucht einen Moment, bis sie beendet ist
            for (int i = 0; i < 20; i++)
            {
                try { File.Delete(old); Toast(T("Alte Datei gelöscht.", "Old file deleted."), ToastKind.Ok); return; }
                catch (UnauthorizedAccessException) { break; }
                catch (IOException) { System.Threading.Thread.Sleep(250); }
            }
            Toast(T("Die alte Datei ließ sich nicht löschen – bitte selbst löschen: ", "Could not delete the old file – please delete it yourself: ") + old, ToastKind.Warn);
        }
    }
}
