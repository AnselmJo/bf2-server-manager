// BotNowDialog.cs - Botzahl der laufenden Map ändern (Map wird neu geladen) - BF2 Manager. C# 5.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BF2Coop
{
    public class BotNowDialog : Form
    {
        static string T(string de, string en) { return I18n.T(de, en); }

        public BotOpt Result;        // null = abgebrochen
        readonly BotEditor _ed; readonly string _map;
        readonly int _pad = Dpi.D(24);

        public BotNowDialog(string mapTitle, BotOpt start, int size, int humansUS, int humansMEC)
        {
            _map = mapTitle;
            FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
            BackColor = Theme.Card; Font = Theme.Body; KeyPreview = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            int w = Dpi.D(560), iw = w - 2 * _pad;

            _ed = new BotEditor();
            _ed.SetContext(size, humansUS, humansMEC);
            _ed.SetOpt(start);
            _ed.Location = new Point(_pad, Dpi.D(150));
            int eh = _ed.LayoutFor(iw);
            _ed.SetBounds(_pad, Dpi.D(150), iw, eh);

            FlatButton bCancel = new FlatButton(T("Abbrechen", "Cancel"), null, BtnKind.Secondary);
            FlatButton bGo = new FlatButton(T("Anwenden und Map neu laden", "Apply and reload map"), G.Refresh, BtnKind.Primary);
            int y = Dpi.D(150) + eh + Dpi.D(18);
            bGo.Location = new Point(w - _pad - bGo.Width, y);
            bCancel.Location = new Point(bGo.Left - Dpi.D(10) - bCancel.Width, y);
            bCancel.Click += delegate { Result = null; Close(); };
            bGo.Click += delegate { Result = _ed.Opt; Close(); };
            ClientSize = new Size(w, y + bGo.Height + _pad);
            foreach (Control c in new Control[] { _ed, bCancel, bGo }) Controls.Add(c);
        }

        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Native.DarkTitle(this); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { Result = null; e.Handled = true; Close(); }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.Clear(Theme.Card);
            int w = ClientSize.Width;
            using (Pen p = new Pen(Theme.BorderHi)) g.DrawRectangle(p, 0, 0, w - 1, ClientSize.Height - 1);
            using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, 0, 0, w, Dpi.D(3));
            Theme.Txt(g, T("Bots ändern", "Change bots"), Theme.Title, new Rectangle(_pad, Dpi.D(16), w - 2 * _pad, Dpi.D(32)), Theme.Text, 0);
            Theme.Txt(g, T("Aktuelle Map: ", "Current map: ") + _map, Theme.BodyB, new Rectangle(_pad, Dpi.D(54), w - 2 * _pad, Dpi.D(22)), Theme.AccentHi, 0);
            TextRenderer.DrawText(g, T("Die Map wird dafür neu geladen (ca. 30 Sekunden); alle Spieler bleiben verbunden, die Runde beginnt von vorn. Die Werte gelten nur für diese Runde – danach gelten wieder die normalen Werte der Rotation.",
                "The map is reloaded for this (about 30 seconds); all players stay connected and the round starts over. The values apply to this round only – afterwards the normal rotation values apply again."),
                Theme.Small, new Rectangle(_pad, Dpi.D(80), w - 2 * _pad, Dpi.D(64)), Theme.Dim, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }
}
