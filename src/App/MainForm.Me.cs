// Hauptfenster: Karte „Dein Spieler“ (Profil, Spielordner, laufendes Spiel) und Ersteinrichtung - BF2 Manager. C# 5.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BF2Coop
{
    public partial class MainForm
    {
        Card _cMe; Para _meName, _meSub, _meInfo, _meGame; TextField _meField; SelectBox _meProfile;
        FlatButton _bMeCreate, _bRename, _bProfMenu, _bPickDir, _bKillGame; ButtonRow _meCreateRow, _meButtons, _meDirRow, _meGameRow;
        ContextMenuStrip _profMenu; bool _gameRunning;

        enum MeState { NoGame, NoProfile, Ready }

        MeState CurrentMeState
        {
            get
            {
                if (!GameLocator.IsGameDir(GameDir)) return MeState.NoGame;
                Bf2Profile p = PlayProfile;
                if (p == null || (!p.Online && Profiles.IsPlaceholder(p.Nick ?? p.Name))) return MeState.NoProfile;
                return MeState.Ready;
            }
        }

        void BuildMe()
        {
            _cMe = new Card(T("Dein Spieler", "Your player"), G.Contact);
            _meName = new Para(""); _meName.Font = Theme.Big; _meName.Color = Theme.Text;
            _meSub = new Para(""); _meSub.Font = Theme.Small;
            _meInfo = new Para(""); _meInfo.Color = Theme.Text;

            _bPickDir = new FlatButton(T("Spielordner wählen …", "Choose game folder …"), G.Folder, BtnKind.Primary);
            _bPickDir.Click += delegate { ChooseGameDir(); };
            _meDirRow = Buttons(_bPickDir);

            _meField = new TextField(G.Contact, T("Dein Name im Spiel", "Your in-game name"));
            _meField.Width = Dpi.D(260); _meField.Height = Dpi.D(34);
            _meField.Box.KeyDown += delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SaveFirstName(); } };
            _bMeCreate = new FlatButton(T("Speichern", "Save"), G.Check, BtnKind.Primary);
            _bMeCreate.Click += delegate { SaveFirstName(); };
            _meCreateRow = Buttons(_meField, _bMeCreate);

            _meProfile = new SelectBox(null, Dpi.D(220)); _meProfile.Height = Dpi.D(34);
            Theme.Tip.SetToolTip(_meProfile, T("Profil wechseln\nJedes Profil hat eigenen Namen, Grafik, Audio und Tasten.", "Switch profile\nEach profile has its own name, graphics, audio and keys."));
            _meProfile.SelectionChanged += delegate
            {
                if (_plLoading) return;
                int i = _meProfile.SelectedIndex;
                if (i < 0 || i >= _profiles.Count) return;
                if (Srv.GameRunning()) { Toast(T("Bitte zuerst Battlefield 2 beenden.", "Please close Battlefield 2 first."), ToastKind.Warn); RefreshMe(); return; }
                UseProfile(_profiles[i]);
            };
            _bRename = new FlatButton(T("Name ändern", "Change name"), G.Edit, BtnKind.Secondary);
            Theme.Tip.SetToolTip(_bRename, T("Name ändern\nGilt sofort – beim nächsten Spielstart heißt du so.", "Change name\nApplies right away – you are called this at the next game start."));
            _bRename.Click += delegate { RenameFlow(); };
            _profMenu = Theme.Menu();
            _profMenu.Opening += delegate { BuildProfileMenu(); };
            _bProfMenu = new FlatButton(T("Profil", "Profile"), G.More, BtnKind.Ghost); _bProfMenu.DropMenu = _profMenu; _bProfMenu.MenuOnly = true;
            Theme.Tip.SetToolTip(_bProfMenu, T("Neues Profil, reparieren, löschen", "New profile, repair, delete"));
            _meButtons = Buttons(_bRename, _meProfile, _bProfMenu);

            _meGame = new Para(""); _meGame.Color = Theme.Dim;
            _bKillGame = new FlatButton(T("Spiel hängt? Beenden", "Game stuck? Close it"), G.Stop, BtnKind.Ghost);
            _bKillGame.Click += delegate { KillGame(); };
            _meGameRow = Buttons(_bKillGame);

            foreach (Control c in new Control[] { _meName, _meSub, _meInfo, _meDirRow, _meCreateRow, _meButtons, _meGame, _meGameRow }) _cMe.Controls.Add(c);
            _cMe.Layouter = Stack(_cMe, delegate
            {
                List<Control> l = new List<Control>();
                MeState st = CurrentMeState;
                if (st == MeState.NoGame) { l.Add(_meInfo); l.Add(_meDirRow); }
                else if (st == MeState.NoProfile) { l.Add(_meInfo); l.Add(_meCreateRow); }
                else { l.Add(_meName); l.Add(_meSub); l.Add(_meButtons); }
                if (_gameRunning) { l.Add(_meGame); l.Add(_meGameRow); }
                return l;
            });
        }

        void RefreshMe()
        {
            if (_cMe == null) return;
            bool old = _plLoading; _plLoading = true;
            _gameRunning = Srv.GameRunning();
            MeState st = CurrentMeState;
            Bf2Profile p = PlayProfile;
            if (st == MeState.NoGame)
                _meInfo.Text = T("Battlefield 2 wurde auf diesem PC nicht gefunden. Wähle den Ordner, in dem BF2.exe liegt.", "Battlefield 2 was not found on this PC. Choose the folder that contains BF2.exe.");
            else if (st == MeState.NoProfile)
            {
                _meInfo.Text = T("Wie heißt du im Spiel? So sehen dich die anderen. Der Manager legt dein Profil dafür an.", "What is your in-game name? This is how others see you. The manager creates your profile for it.");
                if (_meField.Value.Length == 0 && p != null && !Profiles.IsPlaceholder(p.Nick)) _meField.Value = p.Nick;
            }
            else
            {
                _meName.Text = p.Nick ?? p.Name;
                _meSub.Text = T("Profil ", "Profile ") + p.Id + (p.Online ? T("  ·  Online-Konto – Name wird im Spiel verwaltet", "  ·  online account – name is managed in the game") : T("  ·  offline", "  ·  offline")) + "  ·  Mod " + PlayMod;
                _bRename.Enabled = !p.Online;
                List<string> names = new List<string>();
                foreach (Bf2Profile x in _profiles) names.Add(x.Nick ?? x.Name);
                _meProfile.SetItems(names, p.Nick ?? p.Name);
                _meProfile.Visible = _profiles.Count > 1;
                if (_profiles.Count > 1) { if (!_meButtons.Controls.Contains(_meProfile)) _meButtons.Controls.Add(_meProfile); _meButtons.Controls.SetChildIndex(_meProfile, 1); }
                else _meButtons.Controls.Remove(_meProfile);
            }
            _meGame.Text = T("Battlefield 2 läuft gerade. Profil-Änderungen sind erst nach dem Beenden möglich.", "Battlefield 2 is running. Profile changes are possible once it is closed.");
            _plLoading = old;
            if (_plHost != null && _page == PgPlayer) _plHost.Masonry(_plCards, 1);
        }

        // Alle 2 s: läuft das Spiel noch? Dann Karte anpassen.
        void UpdateGameState()
        {
            if (_cMe == null || _page != PgPlayer) return;
            bool run = Srv.GameRunning();
            if (run != _gameRunning) RefreshMe();
        }

        void BuildProfileMenu()
        {
            _profMenu.Items.Clear();
            Bf2Profile p = PlayProfile;
            _profMenu.Items.Add(T("Neues Profil …", "New profile …"), null, delegate { CreateProfileFlow(); });
            ToolStripItem rep = _profMenu.Items.Add(T("Profil reparieren …", "Repair profile …"), null, delegate { RepairFlow(); });
            ToolStripItem del = _profMenu.Items.Add(T("Profil löschen …", "Delete profile …"), null, delegate { DeleteFlow(); });
            rep.ToolTipText = T("Hilft, wenn BF2 beim Start oder Beitreten hängt.", "Helps when BF2 hangs at start or when joining.");
            rep.Enabled = del.Enabled = p != null;
            _profMenu.Items.Add(new ToolStripSeparator());
            _profMenu.Items.Add(T("Profilordner öffnen", "Open profile folder"), null, delegate
            {
                string d = p != null ? p.Dir : Profiles.Root;
                try { if (Directory.Exists(d)) Run(new ProcessStartInfo("explorer.exe", "\"" + d + "\"")); } catch (Exception ex) { Toast(ex.Message, ToastKind.Error); }
            });
            _profMenu.Items.Add(T("Ersteinrichtung erneut …", "Run first-time setup again …"), null, delegate { ShowWelcome(true); });
        }

        bool GameBlocks()
        {
            if (!Srv.GameRunning()) return false;
            Toast(T("Bitte zuerst Battlefield 2 beenden – das Spiel überschreibt sonst das Profil.", "Please close Battlefield 2 first – otherwise the game overwrites the profile."), ToastKind.Warn);
            return true;
        }

        // Fragt nach einem Namen, bis er passt oder abgebrochen wird
        string AskName(string title, string msg, string def, Bf2Profile self)
        {
            string hint = msg;
            while (true)
            {
                string raw = DarkDialog.Input(this, title, hint, def);
                if (raw == null) return null;
                string n = Profiles.CleanName(raw);
                string err = Profiles.NameProblem(n, self);
                if (err == null) return n;
                hint = "⚠ " + err + "\n\n" + msg; def = raw;
            }
        }

        string NameMsg { get { return T("So sehen dich die anderen im Spiel.", "This is how others see you in the game.") + T("\n\n3–24 Zeichen; Leerzeichen werden zu _.", "\n\n3–24 characters; spaces become _."); } }

        void UseProfile(Bf2Profile p)
        {
            if (p == null) return;
            _cfg.Set("PlayProfile", p.Id); _cfg.Save();
            try { Profiles.SetDefault(p.Id); } catch { }
            _profileBackedUp = false;
            AfterProfileChange();
        }

        void AfterProfileChange()
        {
            _profiles = Profiles.All();
            RefreshPlayer();
            RefreshChecks(true);
        }

        // Erster Name (aus der Karte): fehlt ein Profil, wird es angelegt; sonst der Platzhalter ersetzt
        void SaveFirstName()
        {
            if (GameBlocks()) return;
            Bf2Profile p = PlayProfile;
            string n = Profiles.CleanName(_meField.Value);
            string err = Profiles.NameProblem(n, p);
            if (err != null) { Toast(err, ToastKind.Warn); _meField.Box.Focus(); return; }
            try
            {
                if (p == null) p = Profiles.Create(n, DocsDir);
                else Profiles.Rename(p, n, DocsDir);
                _meField.Value = "";
                UseProfile(p);
                Toast(T("Hallo ", "Hello ") + n + "!", ToastKind.Ok);
            }
            catch (Exception ex) { Toast(T("Profil nicht gespeichert: ", "Profile not saved: ") + ex.Message, ToastKind.Error); }
        }

        void RenameFlow()
        {
            Bf2Profile p = PlayProfile;
            if (p == null || p.Online || GameBlocks()) return;
            string cur = p.Nick ?? p.Name;
            string n = AskName(T("Name ändern", "Change name"), NameMsg, Profiles.IsPlaceholder(cur) ? "" : cur, p);
            if (n == null || n == p.Nick) return;
            try { Profiles.Rename(p, n, DocsDir); AfterProfileChange(); Toast(T("Du heißt jetzt ", "Your name is now ") + n + ".", ToastKind.Ok); }
            catch (Exception ex) { Toast(T("Name nicht gespeichert: ", "Name not saved: ") + ex.Message, ToastKind.Error); }
        }

        void CreateProfileFlow()
        {
            if (GameBlocks()) return;
            string n = AskName(T("Neues Profil", "New profile"), NameMsg, "", null);
            if (n == null) return;
            try { Bf2Profile p = Profiles.Create(n, DocsDir); UseProfile(p); Toast(T("Profil „", "Profile “") + n + T("“ angelegt.", "” created."), ToastKind.Ok); }
            catch (Exception ex) { Toast(T("Profil nicht angelegt: ", "Profile not created: ") + ex.Message, ToastKind.Error); }
        }

        void RepairFlow()
        {
            Bf2Profile p = PlayProfile;
            if (p == null || GameBlocks()) return;
            string name = p.Nick ?? p.Name;
            if (!DarkDialog.Confirm(this, T("Profil reparieren", "Repair profile"),
                T("Setzt das Profil „", "Resets the profile “") + name + T("“ zurück: frische Dateien, Grafik, Audio und Tasten auf Standard. Der Name bleibt.\n\nHilft, wenn Battlefield 2 beim Start oder Beitreten hängt.\nEine Sicherung kommt nach Dokumente\\BF2_Backups.", "”: fresh files, graphics, audio and keys back to default. The name stays.\n\nHelps when Battlefield 2 hangs at start or when joining.\nA backup goes to Documents\\BF2_Backups."),
                T("Reparieren", "Repair"), false)) return;
            if (p.Online || Profiles.IsPlaceholder(name))
            {
                name = AskName(T("Profil reparieren", "Repair profile"), NameMsg, Profiles.IsPlaceholder(name) ? "" : name, p);
                if (name == null) return;
            }
            try { Bf2Profile r = Profiles.Repair(p, name, DocsDir); UseProfile(r); Toast(T("Profil repariert. Beim nächsten Start legt BF2 die übrigen Einstellungen neu an.", "Profile repaired. BF2 recreates the remaining settings at the next start."), ToastKind.Ok); }
            catch (Exception ex) { Toast(T("Reparatur fehlgeschlagen: ", "Repair failed: ") + ex.Message, ToastKind.Error); }
        }

        void DeleteFlow()
        {
            Bf2Profile p = PlayProfile;
            if (p == null || GameBlocks()) return;
            if (!DarkDialog.Confirm(this, T("Profil löschen", "Delete profile"),
                T("Profil „", "Delete the profile “") + (p.Nick ?? p.Name) + T("“ löschen? Eine Sicherung kommt nach Dokumente\\BF2_Backups.", "”? A backup goes to Documents\\BF2_Backups."),
                T("Löschen", "Delete"), true)) return;
            try
            {
                Profiles.Delete(p, DocsDir);
                _cfg.Set("PlayProfile", ""); _cfg.Save();
                AfterProfileChange();
                Toast(T("Profil gelöscht.", "Profile deleted."), ToastKind.Ok);
            }
            catch (Exception ex) { Toast(T("Löschen fehlgeschlagen: ", "Delete failed: ") + ex.Message, ToastKind.Error); }
        }

        // Spielordner wählen – von der Spielerkarte, den Einstellungen und der Server-Einrichtung
        void ChooseGameDir()
        {
            string d = WelcomeDialog.PickFolder(this, GameDir);
            if (d == null) return;
            SetGameDir(d);
        }

        void SetGameDir(string d)
        {
            if (string.Equals(d, GameDir, StringComparison.OrdinalIgnoreCase)) return;
            _cfg.Set("GameDir", d); _cfg.Save();
            RefreshStatus(); RefreshPlayer(); RefreshChecks(true);
            Toast(T("Spielordner: ", "Game folder: ") + d, ToastKind.Ok);
        }

        void KillGame()
        {
            if (!DarkDialog.Confirm(this, T("Battlefield 2 beenden", "Close Battlefield 2"),
                T("Battlefield 2 sofort beenden?\n\nWenn das Spiel öfter hängt:\n• Profil reparieren (Menü „Profil“)\n• ReShade ausschalten (Einstellungen › Grafik)\n• Fenstermodus testen (Einstellungen › Spielstart)\n• Auf dem Server-PC prüfen, ob der Server noch läuft",
                  "Close Battlefield 2 right away?\n\nIf the game hangs often:\n• Repair the profile (“Profile” menu)\n• Switch ReShade off (Settings › Graphics)\n• Try windowed mode (Settings › Game start)\n• Check on the server PC that the server is still running"),
                T("Beenden", "Close game"), true)) return;
            int n = 0;
            try { foreach (Process p in Process.GetProcessesByName("BF2")) { try { p.Kill(); n++; } catch { } p.Dispose(); } }
            catch (Exception ex) { Toast(ex.Message, ToastKind.Error); }
            Toast(n > 0 ? T("Battlefield 2 beendet.", "Battlefield 2 closed.") : T("Battlefield 2 lief nicht mehr.", "Battlefield 2 was not running anymore."), ToastKind.Info);
            RefreshMe();
        }

        // Ersteinrichtung beim ersten Start (oder auf Wunsch): Spielordner und Name in einem Fenster
        void ShowWelcome(bool force)
        {
            if (_shotDir != null) return;
            if (!force)
            {
                if (_cfg.B("WelcomeDone", false)) return;
                if (CurrentMeState == MeState.Ready) { _cfg.Set("WelcomeDone", true); _cfg.Save(); return; }
            }
            if (GameBlocks()) return;
            Bf2Profile p = GameLocator.IsGameDir(GameDir) ? PlayProfile : null;
            using (WelcomeDialog d = new WelcomeDialog(GameDir, p))
            {
                DialogResult res = d.ShowDialog(this);
                _cfg.Set("WelcomeDone", true); _cfg.Save();
                if (res != DialogResult.OK) { SelectPage(PgPlayer); return; }
                if (!string.Equals(d.GameDir, GameDir, StringComparison.OrdinalIgnoreCase)) { _cfg.Set("GameDir", d.GameDir); _cfg.Save(); RefreshStatus(); }
                try
                {
                    p = PlayProfile;
                    if (d.PlayerName != null)
                    {
                        if (p == null) p = Profiles.Create(d.PlayerName, DocsDir);
                        else if (d.PlayerName != p.Nick) Profiles.Rename(p, d.PlayerName, DocsDir);
                    }
                    if (p != null) UseProfile(p); else AfterProfileChange();
                    if (d.PlayerName != null) Toast(T("Hallo ", "Hello ") + d.PlayerName + T("! Alles bereit.", "! All set."), ToastKind.Ok);
                }
                catch (Exception ex) { Toast(T("Profil nicht gespeichert: ", "Profile not saved: ") + ex.Message, ToastKind.Error); }
                if (!_host) SelectPage(PgPlayer);
            }
        }
    }
}
