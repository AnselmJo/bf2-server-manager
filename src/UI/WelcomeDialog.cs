// WelcomeDialog.cs - Ersteinrichtung: Spielordner und Spielername in einem Schritt - BF2 Manager. C# 5.
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BF2Coop
{
    public class WelcomeDialog : Form
    {
        static string T(string de, string en) { return I18n.T(de, en); }

        public string GameDir, PlayerName;
        readonly Bf2Profile _profile;
        readonly TextLabel _dirLabel; readonly FlatButton _bDir, _bGo; readonly TextField _name; readonly Para _nameHint;
        readonly int _pad = Dpi.D(28);

        public WelcomeDialog(string gameDir, Bf2Profile profile)
        {
            GameDir = gameDir; _profile = profile;
            FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
            BackColor = Theme.Card; Font = Theme.Body; KeyPreview = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            int w = Dpi.D(560), iw = w - 2 * _pad;
            ClientSize = new Size(w, Dpi.D(420));

            _dirLabel = new TextLabel("", Theme.BodyB); _dirLabel.MaxWidth = iw - Dpi.D(150);
            _dirLabel.Location = new Point(_pad + Dpi.D(36), Dpi.D(132));
            _bDir = new FlatButton(T("Ändern …", "Change …"), G.Folder, BtnKind.Secondary);
            _bDir.Location = new Point(w - _pad - _bDir.Width, Dpi.D(130));
            _bDir.Click += delegate { string d = PickFolder(this, GameDir); if (d != null) { GameDir = d; Validate2(); } };

            _name = new TextField(G.Contact, T("Dein Name im Spiel", "Your in-game name"));
            _name.SetBounds(_pad + Dpi.D(36), Dpi.D(232), iw - Dpi.D(36), Dpi.D(36));
            string cur = profile != null ? (profile.Nick ?? profile.Name ?? "") : "";
            _name.Value = Profiles.IsPlaceholder(cur) ? "" : cur;
            _name.Enabled = profile == null || !profile.Online;
            _name.ValueChanged += delegate { Validate2(); };
            _name.Box.KeyDown += delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter && _bGo.Enabled) { e.SuppressKeyPress = true; Finish(); } };
            _nameHint = new Para(""); _nameHint.Font = Theme.Small;
            _nameHint.SetBounds(_pad + Dpi.D(36), Dpi.D(274), iw - Dpi.D(36), Dpi.D(40));

            FlatButton bLater = new FlatButton(T("Später", "Later"), null, BtnKind.Ghost);
            _bGo = new FlatButton(T("Los geht's", "Let's go"), G.Play, BtnKind.Primary);
            _bGo.Location = new Point(w - _pad - _bGo.Width, ClientSize.Height - _pad - _bGo.Height);
            bLater.Location = new Point(_bGo.Left - Dpi.D(10) - bLater.Width, _bGo.Top);
            bLater.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            _bGo.Click += delegate { Finish(); };

            foreach (Control c in new Control[] { _dirLabel, _bDir, _name, _nameHint, bLater, _bGo }) Controls.Add(c);
            Validate2();
        }

        bool DirOk { get { return GameLocator.IsGameDir(GameDir); } }

        string NameError
        {
            get
            {
                if (_profile != null && _profile.Online) return null;
                return Profiles.NameProblem(Profiles.CleanName(_name.Value), _profile);
            }
        }

        void Validate2()
        {
            _dirLabel.Text = DirOk ? GameDir : T("Nicht gefunden – bitte den Ordner mit BF2.exe wählen", "Not found – please pick the folder containing BF2.exe");
            _dirLabel.Color = DirOk ? Theme.Text : Theme.Red;
            _bDir.Text = DirOk ? T("Ändern …", "Change …") : T("Ordner wählen …", "Choose folder …");
            _bDir.Kind = DirOk ? BtnKind.Secondary : BtnKind.Primary;
            _bDir.Location = new Point(ClientSize.Width - _pad - _bDir.Width, _bDir.Top);
            string err = NameError, clean = Profiles.CleanName(_name.Value);
            if (_profile != null && _profile.Online) { _nameHint.Text = T("Online-Profil – der Name wird im Spiel verwaltet.", "Online profile – the name is managed in the game."); _nameHint.Color = Theme.Dim; }
            else if (_name.Value.Trim().Length == 0) { _nameHint.Text = T("So sehen dich die anderen. 3 bis 24 Zeichen, Leerzeichen werden zu _.", "This is how others see you. 3 to 24 characters, spaces become _."); _nameHint.Color = Theme.Dim; }
            else if (err != null) { _nameHint.Text = err; _nameHint.Color = Theme.Amber; }
            else { _nameHint.Text = clean != _name.Value.Trim() ? T("Wird gespeichert als: ", "Will be saved as: ") + clean : "✓"; _nameHint.Color = Theme.Green; }
            _bGo.Enabled = DirOk && err == null;
            Invalidate();
        }

        void Finish()
        {
            if (!_bGo.Enabled) return;
            PlayerName = _profile != null && _profile.Online ? null : Profiles.CleanName(_name.Value);
            DialogResult = DialogResult.OK; Close();
        }

        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Native.DarkTitle(this); }
        protected override void OnShown(EventArgs e) { base.OnShown(e); if (_name.Enabled) _name.Box.Focus(); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; e.Handled = true; Close(); }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.Clear(Theme.Card); Theme.Hq(g);
            int w = ClientSize.Width;
            using (Pen p = new Pen(Theme.BorderHi)) g.DrawRectangle(p, 0, 0, w - 1, ClientSize.Height - 1);
            using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, 0, 0, w, Dpi.D(3));
            Theme.DrawLogo(g, new Rectangle(_pad, Dpi.D(24), Dpi.D(26), Dpi.D(30)), Theme.Accent);
            Theme.Txt(g, T("Willkommen beim BF2 Manager", "Welcome to BF2 Manager"), Theme.Title, new Rectangle(_pad + Dpi.D(38), Dpi.D(22), w, Dpi.D(34)), Theme.Text, 0);
            Theme.Txt(g, T("Zwei Angaben, dann kann es losgehen. Beides lässt sich später ändern.", "Two things and you are ready. Both can be changed later."), Theme.Body, new Rectangle(_pad, Dpi.D(66), w - 2 * _pad, Dpi.D(22)), Theme.Dim, 0);
            Step(g, 1, T("Spielordner", "Game folder"), Dpi.D(104), DirOk);
            Step(g, 2, T("Dein Spielername", "Your player name"), Dpi.D(204), NameError == null);
        }

        void Step(Graphics g, int n, string title, int y, bool done)
        {
            Rectangle c = new Rectangle(_pad, y, Dpi.D(24), Dpi.D(24));
            using (SolidBrush b = new SolidBrush(done ? Theme.Green : Theme.CardHi)) g.FillEllipse(b, c);
            Theme.Txt(g, done ? "✓" : n.ToString(), Theme.SmallB, c, done ? Theme.Bg : Theme.Text, TextFormatFlags.HorizontalCenter);
            Theme.Txt(g, title, Theme.Head, new Rectangle(_pad + Dpi.D(36), y, ClientSize.Width, Dpi.D(24)), Theme.Text, 0);
        }

        // Ordnerauswahl mit Hilfe: auch der übergeordnete Ordner oder BF2.exe-Ordner eines Unterordners wird erkannt
        public static string PickFolder(IWin32Window owner, string current)
        {
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                fb.Description = T("Battlefield-2-Ordner wählen (der Ordner, in dem BF2.exe liegt)", "Select the Battlefield 2 folder (the one containing BF2.exe)");
                if (GameLocator.IsGameDir(current)) fb.SelectedPath = current;
                if (fb.ShowDialog(owner) != DialogResult.OK) return null;
                string p = fb.SelectedPath.TrimEnd('\\', '/');
                if (GameLocator.IsGameDir(p)) return p;
                string sub = Path.Combine(p, "Battlefield 2");
                if (GameLocator.IsGameDir(sub)) return sub;
                DarkDialog.Info(owner, T("Falscher Ordner", "Wrong folder"), T("In diesem Ordner liegt keine BF2.exe.\nGesucht ist der Ordner, in dem Battlefield 2 installiert ist, z. B. C:\\Program Files (x86)\\EA Games\\Battlefield 2.", "This folder does not contain BF2.exe.\nPick the folder Battlefield 2 is installed in, e.g. C:\\Program Files (x86)\\EA Games\\Battlefield 2."));
                return null;
            }
        }
    }
}
