// Hauptfenster: Menü "Mehr" - BF2 Manager. C# 5 (.NET Framework 4.x).
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
    public partial class MainForm
    {
        // ================================================================== Menü "Mehr"
        // "⋯"-Menü: nur Hilfe – Presets liegen in der Server-Ansicht
        void BuildMoreMenu()
        {
            _moreMenu.Items.Clear();
            ToolStripMenuItem guide = new ToolStripMenuItem(T("Anleitung öffnen", "Open user guide"));
            guide.Click += delegate { OpenUrl(AppInfo.RepoUrl + "/blob/main/docs/USER-GUIDE.md"); };
            ToolStripMenuItem lanGuide = new ToolStripMenuItem(T("LAN-Party vorbereiten", "Prepare a LAN party"));
            lanGuide.Click += delegate { OpenUrl(AppInfo.RepoUrl + "/blob/main/docs/LAN-PARTY.md"); };
            ToolStripMenuItem bug = new ToolStripMenuItem(T("Problem melden", "Report a problem"));
            bug.Click += delegate { OpenUrl(AppInfo.RepoUrl + "/issues/new/choose"); };
            ToolStripMenuItem about = new ToolStripMenuItem(AppInfo.Name + " " + AppInfo.Version); about.Enabled = false;
            _moreMenu.Items.AddRange(new ToolStripItem[] { guide, lanGuide, bug, new ToolStripSeparator(), about });
        }

        static void OpenUrl(string u) { try { Process.Start(u); } catch { } }

        // Schnappschuss der Server-Konfiguration (für Presets, „Zuletzt gestartet“ und den Standard)
        Dictionary<string, object> Snapshot(bool withMaps)
        {
            CollectState();
            Dictionary<string, object> d = new Dictionary<string, object>();
            d["Mod"] = _mod; d["Type"] = _coop ? "coop" : "conquest";
            d["Vals"] = new Dictionary<string, string>(_vals);
            d["HumansUS"] = _st.HumansUS; d["HumansMEC"] = _st.HumansMEC;
            d["Adaptive"] = _st.Adaptive; d["AdaptStep"] = _st.AdaptStep; d["AdaptMax"] = _st.AdaptMax;
            Dictionary<string, string> defs = new Dictionary<string, string>();
            foreach (KeyValuePair<int, BotOpt> kv in _st.Defaults) defs[kv.Key.ToString()] = kv.Value.ToTokens();
            d["Defaults"] = defs;
            if (withMaps)
            {
                List<string> maps = new List<string>();
                foreach (MapEntry m in _st.Maps) maps.Add(m.Name + " " + m.Mode + " " + m.Size + (m.Opt != null ? " " + m.Opt.ToTokens() : ""));
                d["Maps"] = maps;
            }
            return d;
        }

        void SavePreset()
        {
            string name = DarkDialog.Input(this, T("Preset speichern", "Save preset"), T("Name für die aktuelle Konfiguration (Einstellungen, Bots, Rotation):", "Name for the current configuration (settings, bots, rotation):"), _mod + (_coop ? " Coop" : " Conquest"));
            if (string.IsNullOrEmpty(name) || name.Trim().Length == 0) return;
            try
            {
                Directory.CreateDirectory(AppPaths.Presets);
                File.WriteAllText(Path.Combine(AppPaths.Presets, Util.SafeName(name.Trim()) + ".json"), new JavaScriptSerializer().Serialize(Snapshot(true)), new UTF8Encoding(false));
                Toast(T("Preset gespeichert.", "Preset saved."), ToastKind.Ok);
            }
            catch (Exception ex) { Toast(ex.Message, ToastKind.Error); }
        }

        // Preset oder „Zuletzt gestartet“ laden; bei anderem Mod/Modus wird vorher dorthin gewechselt
        void LoadPreset(string file)
        {
            try
            {
                Dictionary<string, object> d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(file, Encoding.UTF8));
                object o;
                string mod = d.TryGetValue("Mod", out o) ? Convert.ToString(o) : _mod;
                bool coop = !d.TryGetValue("Type", out o) || Convert.ToString(o) != "conquest";
                if (_mods.Contains(mod) && (mod != _mod || coop != _coop)) { _selMod.SetSilent(mod); _segType.SetSilent(coop ? 0 : 1); SwitchContext(mod, coop); }
                ApplySnapshot(d, true);
                Toast(T("Konfiguration geladen.", "Configuration loaded."), ToastKind.Ok);
            }
            catch (Exception ex) { Toast(ex.Message, ToastKind.Error); }
        }

        void ApplySnapshot(Dictionary<string, object> d, bool withMaps)
        {
            try
            {
                object o;
                _applying = true;
                if (d.TryGetValue("Vals", out o) && o is Dictionary<string, object>)
                    foreach (KeyValuePair<string, object> kv in (Dictionary<string, object>)o)
                        if (Defs.Get(kv.Key) != null && kv.Value != null) _vals[Defs.Get(kv.Key).Key] = kv.Value.ToString();
                ApplyVals();
                if (d.TryGetValue("HumansUS", out o)) _hUS.SetSilent(Util.ToInt(Convert.ToString(o), 8));
                if (d.TryGetValue("HumansMEC", out o)) _hMEC.SetSilent(Util.ToInt(Convert.ToString(o), 0));
                if (d.TryGetValue("Defaults", out o) && o is Dictionary<string, object>)
                    foreach (KeyValuePair<string, object> kv in (Dictionary<string, object>)o)
                    {
                        BotEditor ed;
                        if (_botEd.TryGetValue(Util.ToInt(kv.Key, 0), out ed)) ed.SetOpt(BotOpt.FromTokens(Convert.ToString(kv.Value).Split(' ')));
                    }
                if (d.TryGetValue("Adaptive", out o) && o is bool) _adaptToggle.SetSilent((bool)o);
                if (d.TryGetValue("AdaptStep", out o)) _adaptStep.SetSilent(Util.ToInt(Convert.ToString(o), 5));
                if (d.TryGetValue("AdaptMax", out o)) _adaptMax.SetSilent(Util.ToInt(Convert.ToString(o), 30));
                if (withMaps && d.TryGetValue("Maps", out o) && o is System.Collections.IEnumerable)
                {
                    List<MapEntry> maps = new List<MapEntry>();
                    foreach (object x in (System.Collections.IEnumerable)o)
                    {
                        string[] p = Convert.ToString(x).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length < 3) continue;
                        MapEntry m = MapEntry.Make(p[0], p[1].ToLowerInvariant(), Util.ToInt(p[2], 0));
                        if (p.Length > 3) m.Opt = BotOpt.FromTokens(Util.Skip(p, 3));
                        if (m.Mode == ModeName && (_avail.Count == 0 || _availIx.ContainsKey(m.Key))) maps.Add(m);
                    }
                    _st.Maps = maps;
                }
                _applying = false;
                UpdateHumans();
                _curRot = null; RefreshRot(_st.Maps.Count > 0 ? 0 : -1); OnRotSelect(); RefreshAvail();
                MarkDirty();
            }
            finally { _applying = false; }
        }
    }
}
