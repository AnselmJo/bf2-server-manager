// Hauptfenster: Grundgerüst - BF2 Manager. C# 5 (.NET Framework 4.x).
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
        // ================================================================== Grundgerüst
        Segmented _segRole; bool _host;

        void BuildShell()
        {
            int rail = Dpi.D(92);
            _header = new HeaderBar(); _header.Dock = DockStyle.Top; _header.Height = Dpi.D(64); _header.BackColor = Theme.Bg; _header.RailW = rail;
            _side = new SideBar(); _side.Dock = DockStyle.Left; _side.Width = rail; _side.BackColor = Theme.Side; _side.Footer = "v" + AppInfo.Version;
            _body = new Panel(); _body.Dock = DockStyle.Fill; _body.BackColor = Theme.Bg;
            Controls.Add(_side); Controls.Add(_header); Controls.Add(_body); _body.BringToFront();

            _segRole = new Segmented(T("Spielen", "Play"), T("Hosten", "Host"));
            Theme.Tip.SetToolTip(_segRole, T("Spielen: Server im Netzwerk finden und beitreten.\nHosten: eigenen Server mit Bots betreiben – inklusive Beitreten.", "Play: find servers on the network and join.\nHost: run your own server with bots – joining included."));
            _segRole.SelectedIndexChanged += delegate { SetRole(_segRole.SelectedIndex == 1); };

            _selMod = new SelectBox("MOD", Dpi.D(180)); _selMod.SetItems(_mods, _mod);
            Theme.Tip.SetToolTip(_selMod, T("Mod des Servers\nEinstellungen, Rotation und Bots gelten je Mod. Weitere Mods unter Einstellungen › Mods.", "Server mod\nSettings, rotation and bots are stored per mod. More mods under Settings › Mods."));
            _selMod.SelectionChanged += delegate { SwitchContext(_selMod.SelectedItem, _coop); };
            _segType = new Segmented("Coop", "Conquest"); _segType.SetSilent(_coop ? 0 : 1);
            Theme.Tip.SetToolTip(_segType, T("Spielmodus\nCoop: Menschen gegen Bots.\nConquest: Eroberung ohne Bots (Mensch gegen Mensch).\n\nWirkt beim nächsten Serverstart.", "Game mode\nCoop: humans vs. bots.\nConquest: no bots (human vs. human).\n\nApplies at the next server start."));
            _segType.SelectedIndexChanged += delegate { SwitchContext(_mod, _segType.SelectedIndex == 0); };

            _pill = new StatusPill();
            _pill.Click += delegate { CopyIp(); };

            _bGame = new FlatButton(T("Beitreten", "Join"), G.Game, BtnKind.Secondary);
            Theme.Tip.SetToolTip(_bGame, T("Meinem Server beitreten\nStartet Battlefield 2 und tritt dem eigenen Server direkt bei.", "Join my server\nStarts Battlefield 2 and joins your own server directly."));
            _bGame.Click += delegate { StartGame(true); };
            _bServer = new FlatButton(T("Server starten", "Start server"), G.Play, BtnKind.Primary);
            Theme.Tip.SetToolTip(_bServer, T("Server starten / stoppen (F5)", "Start / stop server (F5)"));
            _bServer.Click += delegate { ToggleServer(); };
            _bSave = new FlatButton(null, G.Save, BtnKind.Ghost); // nicht sichtbar: es wird automatisch gespeichert

            _moreMenu = Theme.Menu();
            _moreMenu.Opening += delegate { BuildMoreMenu(); };
            _bMore = new FlatButton(null, G.More, BtnKind.Ghost); _bMore.DropMenu = _moreMenu; _bMore.MenuOnly = true;
            Theme.Tip.SetToolTip(_bMore, T("Mehr: Presets, Hilfe", "More: presets, help"));

            foreach (Control c in new Control[] { _segRole, _selMod, _segType, _pill, _bGame, _bServer, _bMore }) _header.Controls.Add(c);
            _header.Resize += delegate { LayoutHeader(); };

            string[] names = { T("Optionen", "Options"), "Bots", "Server", "Live", T("Einstellungen", "Settings"), T("Spielen", "Play") };
            string[] glyphs = { G.Settings, G.Robot, G.Power, G.Pulse, G.Wrench, G.Game };
            for (int i = 0; i < PageCount; i++)
            {
                int idx = i;
                NavButton n = new NavButton(names[i], glyphs[i]);
                n.Width = rail; n.Click += delegate { if (idx == PgMaps) ShowServerView(); else SelectPage(idx); };
                _nav[i] = n; _side.Controls.Add(n);
                Panel p = new Panel(); p.Dock = DockStyle.Fill; p.BackColor = Theme.Bg; p.Visible = false;
                _pages[i] = p; _body.Controls.Add(p);
            }
            _nav[PgLive].Visible = false;
            Theme.Tip.SetToolTip(_nav[PgServer], T("Alle Servereinstellungen", "All server settings"));
            _side.Resize += delegate { LayoutNav(); };
            _toast = new Toast(); Controls.Add(_toast); _toast.BringToFront();
        }

        // Leiste je Rolle: Spielen | Server, Bots, Optionen – Einstellungen immer unten
        void LayoutNav()
        {
            int rail = _side.Width;
            int[] top = _host ? new int[] { PgMaps, PgBots, PgServer } : new int[] { PgPlayer };
            foreach (NavButton n in _nav) n.Visible = false;
            for (int k = 0; k < top.Length; k++) { _nav[top[k]].Visible = true; _nav[top[k]].SetBounds(0, Dpi.D(10) + k * Dpi.D(66), rail - 1, Dpi.D(62)); }
            _nav[PgExtras].Visible = true;
            _nav[PgExtras].SetBounds(0, _side.Height - Dpi.D(104), rail - 1, Dpi.D(62));
        }

        void SetRole(bool host)
        {
            if (host && _setupMode)
            {
                // Erstes Hosten: zuerst einrichten
                _segRole.SetSilent(0); _setupForce = true;
                SelectPage(PgExtras); _exHost.Masonry(_exCards, 2); _exHost.EnsureVisible(_cSetup);
                Toast(T("Zum Hosten einmal den Server einrichten: Mod wählen und „Jetzt einrichten“.", "To host, set up the server once: pick a mod and click “Set up now”."), ToastKind.Info);
                return;
            }
            _host = host;
            _segRole.SetSilent(host ? 1 : 0);
            _cfg.Set("Role", host ? "host" : "play"); _cfg.Save();
            LayoutNav(); LayoutHeader();
            ApplyRoleCards();
            if (host) ShowServerView(); else SelectPage(PgPlayer);
        }

        // Server-Bereich: läuft der Server, ist Live die Hauptansicht, sonst die Konfiguration
        void ShowServerView() { SelectPage(_running && !_forceConfig ? PgLive : PgMaps); }
        bool _forceConfig;

        void LayoutHeader()
        {
            int h = _header.Height;
            _segRole.Location = new Point(_header.RailW + Dpi.D(118), (h - _segRole.Height) / 2);
            foreach (Control c in new Control[] { _selMod, _segType, _pill, _bGame, _bServer }) c.Visible = _host;
            int r = _header.Width - Dpi.D(16);
            _bMore.Location = new Point(r - _bMore.Width, (h - _bMore.Height) / 2); r -= _bMore.Width + Dpi.D(10);
            if (!_host) return;
            for (int lvl = 0; lvl < 3; lvl++)
            {
                _selMod.Width = lvl >= 2 ? Dpi.D(140) : Dpi.D(180);
                _bGame.Text = lvl >= 1 ? null : T("Beitreten", "Join");
                _selMod.Location = new Point(_segRole.Right + Dpi.D(16), (h - _selMod.Height) / 2);
                _segType.Location = new Point(_selMod.Right + Dpi.D(8), (h - _segType.Height) / 2);
                int x = r;
                foreach (Control c in new Control[] { _bServer, _bGame }) { x -= c.Width; c.Location = new Point(x, (h - c.Height) / 2); x -= Dpi.D(8); }
                bool pill = lvl == 0;
                if (pill) { x -= _pill.Width + Dpi.D(4); _pill.Location = new Point(x, (h - _pill.Height) / 2); }
                _pill.Visible = pill;
                if (x >= _segType.Right + Dpi.D(10)) break;
            }
            _bGame.Enabled = _running;
        }

        PageHeader NewPage(int i, string title, string sub)
        {
            PageHeader h = new PageHeader(title, sub);
            _pages[i].Controls.Add(h);
            return h;
        }

        // Kopfzeile oben, dann Hinweisleiste, dann Inhalt
        static void Dock3(Panel page, PageHeader header, Control banner, Control fill)
        {
            if (banner != null) page.Controls.Add(banner);
            page.Controls.Add(fill); fill.BringToFront();
            page.Controls.SetChildIndex(header, page.Controls.Count - 1);
        }

        void SelectPage(int i)
        {
            if (!_nav[i].Enabled) i = _setupMode ? PgPlayer : PgMaps;
            if (!_host && (i == PgServer || i == PgBots || i == PgMaps || i == PgLive)) i = PgPlayer;
            if (_host && i == PgPlayer) i = PgMaps;
            if (i == _page) return;
            _page = i;
            for (int k = 0; k < PageCount; k++) _nav[k].Selected = k == i || (k == PgMaps && i == PgLive);
            _pages[i].Visible = true; _pages[i].BringToFront();
            for (int k = 0; k < PageCount; k++) if (k != i) _pages[k].Visible = false;
            if (_shotDir == null) { _cfg.Set("LastPage", i); }
            if (i == 2) LayoutMaps();
            if (i == 3) { LayoutLive(); RefreshLive(); }
            if (i == PgExtras) { RefreshStatus(); RefreshChecks(false); _exHost.Masonry(_exCards, 2); }
            if (i == PgPlayer) { RefreshPlayer(); _plHost.Masonry(_plCards, 2); }
        }

        Card MakeCard(string key, string title, string glyph, bool collapsible)
        {
            Card c = new Card(title, glyph);
            c.Key = key; c.Collapsible = collapsible; c.Collapsed = collapsible && _collapsed.Contains(key);
            return c;
        }

        void HookCollapse(Card c, ScrollHost host, List<Card> cards, int cols)
        {
            c.Toggled += delegate
            {
                if (c.Collapsed) _collapsed.Add(c.Key); else _collapsed.Remove(c.Key);
                _cfg.Set("Collapsed", new List<string>(_collapsed)); _cfg.Save();
                host.Masonry(cards, cols);
                if (!c.Collapsed) host.EnsureVisible(c);
            };
        }
    }
}
