// CommandDialog.cs - Assistent für Konsolenbefehle: suchen, auswählen, Wert eintragen, ausführen - BF2 Manager. C# 5.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BF2Coop
{
    public class CommandDialog : Form
    {
        static string T(string de, string en) { return I18n.T(de, en); }

        public int Result = -1;          // 1 = in Konsole übernehmen, 2 = ausführen
        public string Command;
        readonly List<CmdDef> _all; List<CmdDef> _view = new List<CmdDef>();
        readonly TextField _search, _param; readonly Segmented _cats; readonly MapListBox _list;
        readonly Para _desc, _paramHint; readonly TextLabel _preview, _paramLabel; readonly FlatButton _bRun, _bFill;
        readonly bool _canRun;

        public CommandDialog(List<CmdDef> commands, bool canRun)
        {
            _all = commands; _canRun = canRun;
            FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
            BackColor = Theme.Card; Font = Theme.Body; KeyPreview = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            int w = Dpi.D(860), h = Dpi.D(580), pad = Dpi.D(24), lw = Dpi.D(400);
            ClientSize = new Size(w, h);

            _search = new TextField(G.Search, T("Befehl oder Stichwort suchen, z. B. Zeit, Tickets, Kick", "Search command or keyword, e.g. time, tickets, kick"));
            _search.SetBounds(pad, Dpi.D(64), lw, Dpi.D(34));
            _search.ValueChanged += delegate { Filter(); };
            List<string> catNames = new List<string> { T("Alle", "All") };
            foreach (string c in Commands.Cats) catNames.Add(Commands.CatName(c));
            _cats = new Segmented(catNames.ToArray());
            _cats.Location = new Point(pad, Dpi.D(106));
            if (_cats.Width > lw) _cats.Width = lw;
            _cats.SelectedIndexChanged += delegate { Filter(); };
            _list = new MapListBox();
            _list.SetBounds(pad, Dpi.D(146), lw, h - Dpi.D(146) - pad);
            _list.SelectedIndexChanged += delegate { ShowSelected(); };
            _list.DoubleClick += delegate { if (_bRun.Enabled) Finish(2); else Finish(1); };

            int rx = pad + lw + Dpi.D(24), rw = w - rx - pad;
            _desc = new Para(""); _desc.Color = Theme.Text; _desc.SetBounds(rx, Dpi.D(64), rw, Dpi.D(150));
            _paramLabel = new TextLabel(T("Wert", "Value"), Theme.BodyB); _paramLabel.Color = Theme.Text; _paramLabel.Location = new Point(rx, Dpi.D(224));
            _param = new TextField(null, ""); _param.SetBounds(rx, Dpi.D(256), rw, Dpi.D(34));
            _param.ValueChanged += delegate { UpdatePreview(); };
            _param.Box.KeyDown += delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Finish(_bRun.Enabled ? 2 : 1); } };
            _paramHint = new Para(""); _paramHint.Color = Theme.Dim; _paramHint.Font = Theme.Small; _paramHint.SetBounds(rx, Dpi.D(296), rw, Dpi.D(40));
            _preview = new TextLabel("", Theme.Mono); _preview.Color = Theme.AccentHi; _preview.MaxWidth = rw; _preview.Location = new Point(rx, Dpi.D(348));
            Para note = new Para(T("Ergebnisse von Abfragen (z. B. admin.listPlayers) erscheinen im Log auf der Live-Seite. Einstellungen gelten bis zum Neustart; dauerhaft über die Seite Server.", "Results of queries (e.g. admin.listPlayers) appear in the log on the Live page. Settings apply until restart; permanently via the Server page."));
            note.Color = Theme.Dim; note.Font = Theme.Small; note.SetBounds(rx, Dpi.D(392), rw, Dpi.D(64));

            _bRun = new FlatButton(T("Ausführen", "Run"), G.Play, BtnKind.Primary);
            _bFill = new FlatButton(T("In Konsole übernehmen", "Put into console"), G.Console, BtnKind.Secondary);
            FlatButton bClose = new FlatButton(T("Schließen", "Close"), null, BtnKind.Ghost);
            int bx = w - pad;
            foreach (FlatButton b in new FlatButton[] { _bRun, _bFill, bClose }) { bx -= b.Width; b.Location = new Point(bx, h - pad - b.Height); bx -= Dpi.D(10); }
            _bRun.Click += delegate { Finish(2); };
            _bFill.Click += delegate { Finish(1); };
            bClose.Click += delegate { Result = -1; Close(); };
            if (!canRun) Theme.Tip.SetToolTip(_bRun, T("Der Server läuft nicht.", "The server is not running."));

            foreach (Control c in new Control[] { _search, _cats, _list, _desc, _paramLabel, _param, _paramHint, _preview, note, _bRun, _bFill, bClose }) Controls.Add(c);
            Filter();
        }

        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Native.DarkScroll(_list); }
        protected override void OnShown(EventArgs e) { base.OnShown(e); _search.Box.Focus(); }

        void Filter()
        {
            string q = (_search.Value ?? "").Trim().ToLowerInvariant();
            string cat = _cats.SelectedIndex > 0 ? Commands.Cats[_cats.SelectedIndex - 1] : null;
            _view = new List<CmdDef>();
            _list.BeginUpdate(); _list.Items.Clear();
            foreach (CmdDef c in _all)
            {
                if (cat != null && c.Cat != cat) continue;
                if (q.Length > 0 && c.Cmd.ToLowerInvariant().IndexOf(q) < 0 && c.Desc.ToLowerInvariant().IndexOf(q) < 0) continue;
                RowView v = new RowView(); v.Title = c.Cmd; v.Sub = c.Desc.Length > 70 ? c.Desc.Substring(0, 68) + " …" : c.Desc;
                v.Badge = Commands.CatName(c.Cat); v.BadgeColor = c.Danger ? Theme.Red : (c.Cat == "settings" ? Theme.Us : Theme.Accent);
                _list.Items.Add(v); _view.Add(c);
            }
            _list.EndUpdate();
            if (_list.Items.Count > 0) _list.SelectedIndex = 0; else ShowSelected();
        }

        public void _TestSearch(string q) { _search.Value = q; }

        CmdDef Sel { get { int i = _list.SelectedIndex; return i >= 0 && i < _view.Count ? _view[i] : null; } }

        void ShowSelected()
        {
            CmdDef c = Sel;
            _desc.Text = c == null ? T("Kein Befehl gefunden.", "No command found.") : c.Cmd + "\n\n" + c.Desc + (c.Danger ? "\n\n" + T("⚠ Wirkt sofort auf alle Spieler.", "⚠ Takes effect for all players immediately.") : "");
            bool p = c != null && c.HasParam;
            _param.Visible = _paramLabel.Visible = _paramHint.Visible = p;
            if (p) { _param.Value = c.Default ?? ""; _paramHint.Text = c.Param ?? ""; }
            UpdatePreview();
        }

        void UpdatePreview()
        {
            CmdDef c = Sel;
            Command = c == null ? null : c.Build(_param.Value);
            _preview.Text = Command == null ? "" : "› " + Command;
            bool ok = c != null && (!c.HasParam || (_param.Value ?? "").Trim().Length > 0);
            _bFill.Enabled = c != null;
            _bRun.Enabled = ok && _canRun;
        }

        void Finish(int r)
        {
            CmdDef c = Sel;
            if (c == null) return;
            if (r == 2 && c.Danger && !DarkDialog.Confirm(this, c.Cmd, T("Diesen Befehl wirklich ausführen?\n", "Really run this command?\n") + Command, T("Ausführen", "Run"), true)) return;
            Result = r; Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { Result = -1; e.Handled = true; Close(); }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.Clear(Theme.Card);
            using (Pen p = new Pen(Theme.BorderHi)) g.DrawRectangle(p, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, 0, 0, ClientSize.Width, Dpi.D(3));
            Theme.Txt(g, T("Konsolenbefehle", "Console commands"), Theme.Title, new Rectangle(Dpi.D(24), Dpi.D(16), ClientSize.Width, Dpi.D(32)), Theme.Text, 0);
        }
    }
}
