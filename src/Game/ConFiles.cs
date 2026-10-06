// .con-Dateien und Mapliste - BF2 Manager. C# 5 (.NET Framework 4.x).
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
    // ------------------------------------------------------------------ .con und Mapliste
    public static class ConFile
    {
        static readonly Regex Rx = new Regex(@"^\s*(sv\.\w+)\s+(.*?)\s*$", RegexOptions.IgnoreCase);
        static readonly Regex RxLine = new Regex(@"^(\s*)(sv\.\w+)(\s+)(.*)$", RegexOptions.IgnoreCase);

        public static Dictionary<string, string> Read(string path)
        {
            Dictionary<string, string> h = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string l in Util.ReadLines(path))
            {
                Match m = Rx.Match(l);
                if (!m.Success) continue;
                string v = m.Groups[2].Value;
                if (v.Length >= 2 && v.StartsWith("\"") && v.EndsWith("\"")) v = v.Substring(1, v.Length - 2);
                h[m.Groups[1].Value] = v;
            }
            return h;
        }

        // Bekannte Zeilen werden ersetzt, unbekannte bleiben erhalten, fehlende werden angehaengt.
        public static void Save(string path, Dictionary<string, string> values)
        {
            List<string> lines = new List<string>(Util.ReadLines(path));
            HashSet<string> done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < lines.Count; i++)
            {
                Match m = RxLine.Match(lines[i]);
                if (!m.Success) continue;
                string k = m.Groups[2].Value;
                string v;
                if (values.TryGetValue(k, out v) && !done.Contains(k)) { lines[i] = m.Groups[1].Value + k + m.Groups[3].Value + v; done.Add(k); }
            }
            foreach (KeyValuePair<string, string> kv in values) if (!done.Contains(kv.Key)) lines.Add(kv.Key + " " + kv.Value);
            Util.WriteAtomic(path, string.Join("\r\n", lines.ToArray()) + "\r\n", Util.Ansi);
        }
    }

    public static class Maplist
    {
        static readonly Regex Rx = new Regex(@"^\s*mapList\.append\s+""?([\w\-]+)""?\s+(\w+)(?:\s+(\d+))?\s*$", RegexOptions.IgnoreCase);

        public static List<MapEntry> Read(string path)
        {
            List<MapEntry> r = new List<MapEntry>();
            foreach (string l in Util.ReadLines(path))
            {
                Match m = Rx.Match(l);
                if (m.Success) r.Add(MapEntry.Make(m.Groups[1].Value, m.Groups[2].Value.ToLowerInvariant(), Util.ToInt(m.Groups[3].Value, 0)));
            }
            return r;
        }

        public static void Write(string path, List<MapEntry> maps)
        {
            StringBuilder sb = new StringBuilder("rem Written by BF2 Manager - order = rotation\r\n");
            foreach (MapEntry m in maps) sb.Append("mapList.append " + m.Name + " " + m.Mode + " " + m.Size + "\r\n");
            Util.WriteAtomic(path, sb.ToString(), Util.Ansi);
        }
    }
}
