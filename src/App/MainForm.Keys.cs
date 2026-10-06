// Hauptfenster: Tastatur, Schließen - BF2 Manager. C# 5 (.NET Framework 4.x).
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
        // ================================================================== Tastatur, Schließen
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S)) { SaveAll(false); return true; }
            if (keyData == Keys.F5) { ToggleServer(); return true; }
            int[] order = _host ? new int[] { PgMaps, PgBots, PgServer, PgExtras } : new int[] { PgPlayer, PgExtras };
            for (int i = 0; i < order.Length; i++) if (keyData == (Keys.Control | (Keys.D1 + i))) { SelectPage(order[i]); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_shotDir == null && !Restart && !_updExit && !ConfirmDiscard()) { e.Cancel = true; return; }
            _timer.Stop();
            StopLan();
            if (WindowState == FormWindowState.Normal) { _cfg.Set("WinW", (int)(Width / Dpi.S)); _cfg.Set("WinH", (int)(Height / Dpi.S)); }
            _cfg.Save();
            base.OnFormClosing(e);
        }
    }
}
