// Hauptfenster: LAN-Dienst, Server-Finder, Abgleich und Sync - BF2 Manager. C# 5.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BF2Coop
{
    public partial class MainForm
    {
        LanListener _lanListen; LanFileServer _lanFiles; DateTime _lastBeacon = DateTime.MinValue;
        List<LanHost> _fakeHosts;
        LanHost _syncHost; SyncPlan _syncPlan; volatile bool _syncCancel; bool _syncBusy;

        bool LanHostOn { get { return _cfg.B("LanHost", !_setupMode); } }

        void StartLan()
        {
            if (_shotDir != null) return;
            _lanListen = new LanListener();
            _lanListen.Start();
            ApplyLanHost();
            RefreshLanInfo();
        }

        void StopLan()
        {
            if (_lanListen != null) { _lanListen.Dispose(); _lanListen = null; }
            if (_lanFiles != null) { _lanFiles.Dispose(); _lanFiles = null; }
        }

        void ApplyLanHost()
        {
            bool on = LanHostOn && GameLocator.IsGameDir(GameDir) && _shotDir == null;
            if (on && _lanFiles == null) { _lanFiles = new LanFileServer(GameDir); _lanFiles.Start(); }
            else if (!on && _lanFiles != null) { _lanFiles.Dispose(); _lanFiles = null; }
        }

        // Alle 2 s aus RefreshLive: Bekanntmachung senden und Serverliste aktualisieren
        void LanTick(Dictionary<string, string> live)
        {
            if (_lanFiles != null && _lanFiles.Error == null && (DateTime.Now - _lastBeacon).TotalSeconds >= 3)
            {
                _lastBeacon = DateTime.Now;
                string map, hu, hm; live.TryGetValue("map", out map);
                int humans = live.TryGetValue("humansUS", out hu) && live.TryGetValue("humansMEC", out hm) ? Util.ToInt(hu, 0) + Util.ToInt(hm, 0) : 0;
                string bu, bm; int bots = live.TryGetValue("botsUS", out bu) && live.TryGetValue("botsMEC", out bm) ? Util.ToInt(bu, 0) + Util.ToInt(bm, 0) : 0;
                int size = 0; foreach (MapEntry me in _st.Maps) if (string.Equals(me.Name, map, StringComparison.OrdinalIgnoreCase)) { size = me.Size; break; }
                string id = _cfg.S("InstanceId", null);
                if (id == null) { id = Guid.NewGuid().ToString("N").Substring(0, 12); _cfg.Set("InstanceId", id); _cfg.Save(); }
                string payload = Lan.Payload(_vals.ContainsKey("sv.serverName") ? _vals["sv.serverName"] : "BF2", Util.ToInt(_vals["sv.serverPort"], 16567),
                    _mod.Length > 0 ? _mod : PlayMod, map ?? "", humans, Util.ToInt(_vals["sv.maxPlayers"], 64), _coop ? "coop" : "cq", _running)
                    + "|" + id + "|" + size + "|" + bots + "|" + Lan.Esc(ModInfo.Read(GameDir, _mod).Version ?? "");
                ThreadPool.QueueUserWorkItem(delegate { try { Lan.Announce(payload); } catch { } });
            }
            if (_page == PgPlayer && !_syncBusy) RefreshServers(false);
        }

        void SetSync(string text) { _syncText.Text = text; if (_page == PgPlayer) _plHost.Masonry(_plCards, 2); }

        // Lokale Mod-Dateien mit dem Server-PC vergleichen
        void CompareWith(LanHost h)
        {
            if (_syncBusy) return;
            _syncBusy = true; _syncCancel = false; _syncHost = h; _syncPlan = null;
            string mod = h.Mod, root = Path.Combine(GameDir, "mods", h.Mod);
            SetSync(T("Hole Dateiliste vom Server-PC … (beim ersten Mal kann das dauern, weil der Server-PC alle Dateien prüft)", "Fetching file list from the server PC … (the first time can take a while because the server PC checks all files)"));
            ThreadPool.QueueUserWorkItem(delegate
            {
                string err = null; SyncPlan plan = null;
                try
                {
                    List<FileEntry> remote = Lan.ParseManifest(Lan.GetText(h, "/manifest?mod=" + Uri.EscapeDataString(mod), 900000));
                    int last = -1;
                    List<FileEntry> local = Lan.BuildManifest(root, delegate (int i, int n)
                    {
                        int pct = n > 0 ? i * 100 / n : 0;
                        if (pct / 5 != last / 5) { last = pct; try { BeginInvoke((MethodInvoker)delegate { SetSync(T("Prüfe eigene Dateien … ", "Checking own files … ") + pct + " %"); }); } catch { } }
                    }, delegate { return _syncCancel; });
                    plan = Lan.Diff(local, remote);
                }
                catch (OperationCanceledException) { err = T("Abgebrochen.", "Cancelled."); }
                catch (Exception ex) { err = ex.Message; }
                try { BeginInvoke((MethodInvoker)delegate { CompareDone(h, plan, err); }); } catch { }
            });
            if (_page == PgPlayer) _plHost.Masonry(_plCards, 2);
        }

        void CompareDone(LanHost h, SyncPlan plan, string err)
        {
            _syncBusy = false; _syncPlan = plan;
            if (err != null) { SetSync("⚠ " + T("Abgleich nicht möglich: ", "Compare failed: ") + err + T("\nFirewall auf dem Server-PC prüfen (PC-Check).", "\nCheck the firewall on the server PC (PC check).")); return; }
            if (plan.Count == 0)
            {
                SetSync("✓ " + T("Alle Dateien von „", "All files of “") + h.Mod + T("“ stimmen mit dem Server-PC überein.", "” match the server PC.") +
                    (plan.Extra.Count > 0 ? "\n" + plan.Extra.Count + T(" zusätzliche Dateien nur auf diesem PC (stören nicht).", " extra files only on this PC (harmless).") : ""));
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("⚠ ").Append(plan.Count).Append(plan.Count == 1 ? T(" Datei weicht ab oder fehlt (", " file differs or is missing (") : T(" Dateien weichen ab oder fehlen (", " files differ or are missing (")).Append(SizeText(plan.Bytes)).Append(")");
            if (plan.Missing.Count > 0) sb.Append(T(" · fehlend: ", " · missing: ")).Append(plan.Missing.Count);
            if (plan.Different.Count > 0) sb.Append(T(" · anders: ", " · different: ")).Append(plan.Different.Count);
            List<FileEntry> all = new List<FileEntry>(plan.Missing); all.AddRange(plan.Different);
            for (int i = 0; i < all.Count && i < 6; i++) sb.Append("\n   ").Append(all[i].Rel);
            if (all.Count > 6) sb.Append("\n   …");
            if (plan.Extra.Count > 0) sb.Append("\n").Append(plan.Extra.Count).Append(T(" zusätzliche Dateien nur auf diesem PC bleiben unverändert.", " extra files only on this PC stay unchanged."));
            SetSync(sb.ToString());
        }

        static string SizeText(long b)
        {
            if (b >= 1L << 30) return (b / (double)(1L << 30)).ToString("0.0") + " GB";
            if (b >= 1L << 20) return (b / (double)(1L << 20)).ToString("0.0") + " MB";
            return Math.Max(1, b / 1024) + " KB";
        }

        // Abweichende und fehlende Dateien vom Server-PC holen (jede Datei geprüft, erst danach ersetzt)
        void SyncFromHost()
        {
            if (_syncBusy || _syncPlan == null || _syncHost == null || _syncPlan.Count == 0) return;
            if (Srv.GameRunning()) { Toast(T("Bitte zuerst Battlefield 2 beenden.", "Please close Battlefield 2 first."), ToastKind.Warn); return; }
            if (_running && string.Equals(_mod, _syncHost.Mod, StringComparison.OrdinalIgnoreCase)) { Toast(T("Bitte zuerst den eigenen Server beenden.", "Please stop your own server first."), ToastKind.Warn); return; }
            if (!DarkDialog.Confirm(this, T("Dateien holen", "Fetch files"), _syncPlan.Count + T(" Dateien (", " files (") + SizeText(_syncPlan.Bytes) + T(") vom Server-PC „", ") from the server PC “") + _syncHost.Name + T("“ in den Mod „", "” into the mod “") + _syncHost.Mod + T("“ übernehmen?\nVorhandene Dateien werden ersetzt.", "”?\nExisting files are replaced."), T("Holen", "Fetch"), false)) return;
            string root = Path.Combine(GameDir, "mods", _syncHost.Mod);
            if (!Installer.CanWrite(GameLocator.IsGameDir(GameDir) ? Path.Combine(GameDir, "mods") : root)) { Toast(T("Keine Schreibrechte – PC-Check › Schreibrechte.", "No write access – PC check › write access."), ToastKind.Error); return; }
            _syncBusy = true; _syncCancel = false;
            LanHost h = _syncHost; SyncPlan plan = _syncPlan; string mod = h.Mod;
            List<FileEntry> todo = new List<FileEntry>(plan.Missing); todo.AddRange(plan.Different);
            long total = plan.Bytes;
            SetSync(T("Übertrage …", "Transferring …"));
            ThreadPool.QueueUserWorkItem(delegate
            {
                long done = 0; int files = 0, last = -1; string err = null;
                try
                {
                    foreach (FileEntry e in todo)
                    {
                        if (_syncCancel) throw new OperationCanceledException();
                        Lan.Fetch(h, mod, e, root, delegate (long n)
                        {
                            done += n;
                            int pct = total > 0 ? (int)(done * 100 / total) : 100;
                            if (pct != last) { last = pct; try { BeginInvoke((MethodInvoker)delegate { SetSync(T("Übertrage … ", "Transferring … ") + pct + " %  (" + SizeText(done) + " / " + SizeText(total) + ")"); }); } catch { } }
                        }, delegate { return _syncCancel; });
                        files++;
                    }
                }
                catch (OperationCanceledException) { err = T("Abgebrochen.", "Cancelled."); }
                catch (Exception ex) { err = ex.Message; }
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _syncBusy = false; _syncPlan = null;
                        if (err != null) { SetSync("⚠ " + files + T(" Dateien übertragen, dann gestoppt: ", " files transferred, then stopped: ") + err); return; }
                        SetSync("✓ " + files + T(" Dateien übernommen. Der Mod entspricht jetzt dem Server-PC.", " files taken over. The mod now matches the server PC."));
                        Toast(T("Abgleich fertig.", "Sync finished."), ToastKind.Ok);
                        RefreshStatus();
                    });
                }
                catch { }
            });
            if (_page == PgPlayer) _plHost.Masonry(_plCards, 2);
        }
    }
}
