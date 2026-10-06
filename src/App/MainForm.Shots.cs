// Hauptfenster: Testmodus: Screenshots - BF2 Manager. C# 5 (.NET Framework 4.x).
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
        // ================================================================== Testmodus: Screenshots
        void RunShots()
        {
            List<Action> steps = new List<Action>();
            LanHost fake = new LanHost(); fake.Ip = "192.168.1.20"; fake.Name = "Ansis LAN Coop"; fake.Mod = "bf2all64"; fake.Map = "strike_at_karkand"; fake.Humans = 6; fake.Max = 64; fake.Running = true; fake.Version = "1.2.1"; fake.GamePort = 16567; fake.HttpPort = Lan.HttpPort;
            LanHost fake2 = new LanHost(); fake2.Ip = "192.168.1.31"; fake2.Name = "Kater PC"; fake2.Mod = "bfp2"; fake2.Map = ""; fake2.Running = false; fake2.Version = "1.2.0"; fake2.GamePort = 16567; fake2.HttpPort = Lan.HttpPort;
            _fakeHosts = new List<LanHost> { fake, fake2 };
            CommandDialog[] dlg = new CommandDialog[1];
            fake.Size = 32; fake.Bots = 24; fake.ModVersion = "1.0"; fake.Id = "a1";
            LanHost fake3 = new LanHost(); fake3.Ip = "192.168.1.44"; fake3.Name = "Pirates Abend"; fake3.Mod = "bfp2"; fake3.Map = "frylar"; fake3.Humans = 2; fake3.Max = 32; fake3.Running = true; fake3.Version = "1.2.1"; fake3.Mode = "cq"; fake3.Size = 32; fake3.Id = "b2"; fake3.GamePort = 16567; fake3.HttpPort = Lan.HttpPort;
            fake.Mode = "coop";
            _fakeHosts = new List<LanHost> { fake, fake2, fake3 };
            steps.Add(delegate { SetRole(true); ShowServerView(); if (_lbRot.Items.Count > 1) _lbRot.SelectedIndex = 1; });
            steps.Add(delegate { Shot("host_server"); SelectPage(PgBots); });
            steps.Add(delegate { Shot("host_bots"); SelectPage(PgServer); });
            steps.Add(delegate { Shot("host_options"); SelectPage(PgExtras); });
            steps.Add(delegate { Shot("host_settings"); Srv.FakeRunning = true; RefreshLive(); ShowServerView(); });
            steps.Add(delegate { Shot("host_live"); _forceConfig = true; SelectPage(PgMaps); });
            steps.Add(delegate { Shot("host_server_running"); SelectPage(PgServer); _vals["sv.maxPlayers"] = "48"; UpdateLiveBanner(); });
            steps.Add(delegate { Shot("host_options_running"); Srv.FakeRunning = false; _userStopped = false; RefreshLive(); });
            steps.Add(delegate { Shot("host_crashed"); SetRole(false); });
            steps.Add(delegate { RefreshServers(true); RefreshPlayHealth(); _plHost.Masonry(_plCards, 1); });
            steps.Add(delegate { Shot("play"); _fakeHosts = new List<LanHost>(); RefreshServers(true); _plHost.Masonry(_plCards, 1); });
            steps.Add(delegate { Shot("play_empty"); SelectPage(PgExtras); });
            steps.Add(delegate { Shot("play_settings"); _exHost.ScrollTo(99999, false); });
            steps.Add(delegate { Shot("play_settings_scrolled"); dlg[0] = new CommandDialog(Commands.All(_vals), true); dlg[0].StartPosition = FormStartPosition.Manual; dlg[0].Location = PointToScreen(new Point(Dpi.D(160), Dpi.D(90))); dlg[0].Show(this); });
            steps.Add(delegate { Shot("commands"); dlg[0].Close(); });
            int step = 0;
            WinTimer t = new WinTimer(); t.Interval = 900;
            t.Tick += delegate
            {
                try { if (step < steps.Count) { steps[step++](); return; } }
                catch (Exception ex) { Util.Log(AppPaths.Dir, ex.ToString()); }
                t.Stop(); _dirty = false; Close();
            };
            t.Start();
        }

        void Shot(string name)
        {
            using (Bitmap b = new Bitmap(ClientSize.Width, ClientSize.Height))
            {
                using (Graphics g = Graphics.FromImage(b)) g.CopyFromScreen(PointToScreen(Point.Empty), Point.Empty, ClientSize);
                Directory.CreateDirectory(_shotDir);
                b.Save(Path.Combine(_shotDir, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
