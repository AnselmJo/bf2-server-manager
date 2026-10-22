// Tests.cs - automatische Tests ohne Test-Framework (läuft in CI unter Windows und lokal mit Mono). C# 5.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BF2Coop.Tests
{
    public static class Runner
    {
        static int _pass, _fail;
        static string _tmp;

        static void Check(bool ok, string name)
        {
            if (ok) { _pass++; Console.WriteLine("PASS  " + name); }
            else { _fail++; Console.WriteLine("FAIL  " + name); }
        }

        static void Run(string name, Action a)
        {
            try { a(); }
            catch (Exception ex) { _fail++; Console.WriteLine("FAIL  " + name + " -> " + ex.GetType().Name + ": " + ex.Message); }
        }

        public static int Main()
        {
            _tmp = Path.Combine(Path.GetTempPath(), "bf2sm_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tmp);
            I18n.En = true;
            Run("settings", Settings);
            Run("planner", Planner_);
            Run("cooptxt", CoopTxtRoundtrip);
            Run("versions", Versions);
            Run("hash", Hash);
            Run("laa", Laa);
            Run("unlock", Unlock);
            Run("lan", LanTests);
            Run("leaderboard", Board);
            Run("profiles", ProfileFiles);
            Run("profilemanage", ProfileManage);
            Run("radio", Radio);
            Run("moddesc", ModDesc);
            Run("commands", Cmds);
            Run("scrub", Scrub);
            try { Directory.Delete(_tmp, true); } catch { }
            Console.WriteLine();
            Console.WriteLine(_pass + " passed, " + _fail + " failed");
            return _fail == 0 ? 0 : 1;
        }

        static void Settings()
        {
            Check(Defs.All.Count > 60, "settings table loaded (" + Defs.All.Count + ")");
            bool groups = true; foreach (SettingDef d in Defs.All) if (Defs.GroupOf(d.Group) == null) groups = false;
            Check(groups, "every setting has a group");
            SettingDef t = Defs.Get("sv.timeLimit");
            Check(Defs.ToCon(t, "30") == "1800" && Defs.FromCon(t, "1800") == "30", "minutes <-> seconds");
            SettingDef ff = Defs.Get("sv.soldierFriendlyFire");
            Check(Defs.ToCon(ff, "1") == "100" && Defs.FromCon(ff, "50") == "1", "friendly fire percent");
        }

        static void Planner_()
        {
            BotOpt o = BotOpt.Make(24, 30);
            BotPlan p = Planner.Plan(o, 6, 4);
            Check(p.Count == 24 && p.Total == 34, "fixed count: 24 bots + 10 humans = 34");
            Check(p.TotalMEC > p.TotalUS, "enemy advantage gives MEC more players (US " + p.TotalUS + " / MEC " + p.TotalMEC + ")");
            BotOpt f = BotOpt.Make(8, 30); f.Mode = "fill"; f.Target = 32;
            BotPlan q = Planner.Plan(f, 8, 0);
            Check(q.Count == 24, "fill up to 32 with 8 humans = 24 bots");
        }

        static void CoopTxtRoundtrip()
        {
            CoopState st = new CoopState();
            st.HumansUS = 5; st.HumansMEC = 2; st.Commander = false; st.Adaptive = true; st.AdaptStep = 7; st.AdaptMax = 21;
            st.Maps.Add(MapEntry.Make("strike_at_karkand", "gpm_coop", 32));
            MapEntry m = MapEntry.Make("gulf_of_oman", "gpm_coop", 64); m.Opt = BotOpt.Make(40, 60); st.Maps.Add(m);
            string f = Path.Combine(_tmp, "coop_server.txt");
            CoopTxt.Write(f, st);
            CoopState r = CoopTxt.Read(f);
            Check(r.HumansUS == 5 && r.HumansMEC == 2 && !r.Commander, "coop_server.txt humans/commander");
            Check(r.Adaptive && r.AdaptStep == 7 && r.AdaptMax == 21, "coop_server.txt adaptive");
            Check(r.Maps.Count == 2 && r.Maps[1].Opt != null && r.Maps[1].Opt.Count == 40 && r.Maps[1].Opt.Diff == 60, "coop_server.txt maps with override");
        }

        static void Versions()
        {
            Check(Updater.Compare("1.2.0", "1.1.1") > 0, "1.2.0 > 1.1.1");
            Check(Updater.Compare("v1.2.0", "1.2.0") == 0, "v prefix ignored");
            Check(Updater.Compare("1.2.0-beta.1", "1.2.0") < 0, "beta < release");
            Check(Updater.Compare("1.2.0-beta.2", "1.2.0-beta.10") < 0, "beta.2 < beta.10");
            Check(Updater.Compare("1.10.0", "1.9.9") > 0, "numeric not lexical");
        }

        static void Hash()
        {
            string h = new string('a', 64);
            Check(Updater.ExpectedHash(h + "  BF2Manager.exe\n") == h, "SHA256SUMS line");
            Check(Updater.ExpectedHash("x  other.exe\n") == null, "SHA256SUMS other file ignored");
            string f = Path.Combine(_tmp, "abc.txt"); File.WriteAllText(f, "abc", Encoding.ASCII);
            Check(Updater.Sha256(f) == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", "sha256 of 'abc'");
        }

        static string FakeExe(string name, byte[] extra)
        {
            byte[] d = new byte[0x200];
            d[0] = (byte)'M'; d[1] = (byte)'Z';
            BitConverter.GetBytes(0x80).CopyTo(d, 0x3C);
            d[0x80] = (byte)'P'; d[0x81] = (byte)'E';
            BitConverter.GetBytes((ushort)0x0102).CopyTo(d, 0x80 + 22);
            if (extra != null) extra.CopyTo(d, 0x100);
            string f = Path.Combine(_tmp, name);
            File.WriteAllBytes(f, d);
            return f;
        }

        static void Laa()
        {
            string f = FakeExe("laa.exe", null);
            Check(Pe.LaaState(f) == PatchState.Off, "LAA off initially");
            Pe.SetLaa(f, true);
            Check(Pe.LaaState(f) == PatchState.On, "LAA on after patch");
            Check(File.Exists(f + ".vor_4gb.bak"), "LAA backup created");
            Pe.SetLaa(f, false);
            Check(Pe.LaaState(f) == PatchState.Off, "LAA off after undo");
        }

        static void Unlock()
        {
            byte[] pat = { 0x8B, 0x75, 0x10, 0x39, 0x70, 0x10, 0x7C, 0x14, 0x8B, 0xCB, 0xE8 };
            string f = FakeExe("BF2.exe", pat);
            Check(ClientUnlock.State(f) == PatchState.Off, "unlock off initially");
            ClientUnlock.Apply(f, true);
            Check(ClientUnlock.State(f) == PatchState.On, "unlock on");
            Pe.SetLaa(f, true);
            ClientUnlock.Apply(f, false);
            Check(ClientUnlock.State(f) == PatchState.Off && Pe.LaaState(f) == PatchState.On, "unlock undo keeps 4 GB patch");
        }

        static void LanTests()
        {
            List<FileEntry> loc = new List<FileEntry>(), rem = new List<FileEntry>();
            loc.Add(E("a.txt", 1, "11")); loc.Add(E("b.txt", 2, "22")); loc.Add(E("only_local.txt", 3, "33"));
            rem.Add(E("a.txt", 1, "11")); rem.Add(E("b.txt", 2, "99")); rem.Add(E("levels\\x\\client.zip", 5, "55"));
            SyncPlan p = Lan.Diff(loc, rem);
            Check(p.Missing.Count == 1 && p.Different.Count == 1 && p.Extra.Count == 1 && p.Bytes == 7, "manifest diff");
            Check(Lan.SafePath(_tmp, "..\\evil.txt") == null && Lan.SafePath(_tmp, "C:\\x") == null && Lan.SafePath(_tmp, "levels\\a.zip") != null, "safe paths");
            Check(Lan.Excluded("settings\\serversettings.con") && Lan.Excluded("python\\game\\coopbots.py") && !Lan.Excluded("levels\\a\\client.zip"), "excluded server-only files");
            Check(Lan.ParseManifest("..\\x|1|aa\nok.txt|2|bb\n").Count == 1, "manifest rejects traversal");
            string root = Path.Combine(_tmp, "mod"); Directory.CreateDirectory(Path.Combine(root, "levels"));
            File.WriteAllText(Path.Combine(root, "levels", "a.txt"), "hello");
            List<FileEntry> m = Lan.BuildManifest(root, null, null);
            Check(m.Count == 1 && m[0].Hash == "aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d", "manifest sha1");
            LanHost h = Lan.Parse(Lan.Payload("My Server", 16567, "bf2all64", "karkand", 3, 64, "coop", true), System.Net.IPAddress.Parse("10.0.0.5"));
            Check(h != null && h.Name == "My Server" && h.Mod == "bf2all64" && h.Running && h.Humans == 3, "beacon payload roundtrip");
            LanHost h2 = Lan.Parse(Lan.Payload("S", 16567, "bf2all64", "karkand", 3, 64, "coop", true) + "|abc123|32|24|1.0", System.Net.IPAddress.Parse("10.0.0.5"));
            LanHost h3 = Lan.Parse(Lan.Payload("S", 16567, "bf2all64", "karkand", 3, 64, "coop", true) + "|abc123|32|24|1.0", System.Net.IPAddress.Parse("10.0.0.6"));
            Check(h2.Size == 32 && h2.Bots == 24 && h2.ModVersion == "1.0" && h2.Key == h3.Key, "beacon v2 fields, one entry per PC");
        }

        static FileEntry E(string rel, long size, string hash) { FileEntry e = new FileEntry(); e.Rel = rel; e.Size = size; e.Hash = hash; return e; }

        static void Board()
        {
            string f = Path.Combine(_tmp, "coop_rounds.txt");
            File.WriteAllText(f, "round|100|map|2|10|0|2\nplayer|Ansi|2|50|10|2\nplayer|Kater|2|30|5|5\nround|200|map|1|0|10|2\nplayer|Ansi|2|20|3|6\n");
            int rounds;
            List<LeaderRow> r = Leaderboard.Read(f, 0, out rounds);
            Check(rounds == 2 && r.Count == 2 && r[0].Name == "Ansi" && r[0].Score == 70 && r[0].Rounds == 2 && r[0].Wins == 1, "leaderboard totals");
            r = Leaderboard.Read(f, 150, out rounds);
            Check(rounds == 1 && r.Count == 1 && r[0].Score == 20, "leaderboard since");
        }

        static void ProfileFiles()
        {
            string f = Path.Combine(_tmp, "Video.con");
            File.WriteAllText(f, "VideoSettings.setResolution 800x600@60Hz\r\nVideoSettings.setViewDistanceScale 0.75\r\n");
            Dictionary<string, string> v = new Dictionary<string, string>();
            v["setResolution"] = "1920x1080@60Hz"; v["setTerrainQuality"] = "2";
            Profiles.Set(f, "VideoSettings", v);
            Check(Profiles.Get(f, "VideoSettings", "setResolution") == "1920x1080@60Hz", "profile value replaced");
            Check(Profiles.Get(f, "VideoSettings", "setTerrainQuality") == "2", "profile value added");
            Check(Profiles.Get(f, "VideoSettings", "setViewDistanceScale") == "0.75", "other values kept");
            Check(Profiles.CleanName(" Ansi der Große ") == "Ansi_der_Groe", "player name cleaned");
        }

        static void ProfileManage()
        {
            string root = Path.Combine(_tmp, "Profiles"), docs = Path.Combine(_tmp, "docs");
            Directory.CreateDirectory(Path.Combine(root, "Default"));
            File.WriteAllText(Path.Combine(root, "Default", "Video.con"), "VideoSettings.setResolution 800x600@60Hz\r\n");
            File.WriteAllText(Path.Combine(root, "Default", "Profile.con"), "LocalProfile.setName \"Default\"\r\n");
            Profiles.RootOverride = root;
            try
            {
                Check(Profiles.All().Count == 0 && Profiles.Default() == null, "template folder Default is not a profile");
                Bf2Profile a = Profiles.Create("Ansi", docs);
                Check(a != null && a.Id == "0001" && a.Nick == "Ansi" && a.Name == "Ansi", "profile created as 0001");
                Check(Profiles.DefaultId() == "0001", "new profile becomes default (Global.con created)");
                Check(File.Exists(Path.Combine(a.Dir, "Video.con")), "template files copied");
                Check(Profiles.NameProblem("ansi", null) != null, "duplicate name rejected");
                Check(Profiles.NameProblem("ansi", a) == null, "own name allowed for the same profile");
                Check(Profiles.NameProblem("defaultPlayer", null) != null && Profiles.NameProblem("ab", null) != null, "placeholder and short names rejected");
                Bf2Profile b = Profiles.Create("Zweiter", docs);
                Check(b.Id == "0002" && Profiles.DefaultId() == "0002", "second profile 0002");
                Profiles.Rename(a, "Anselm", docs);
                Bf2Profile a2 = null; foreach (Bf2Profile p in Profiles.All()) if (p.Id == "0001") a2 = p;
                Check(a2 != null && a2.Name == "Anselm" && a2.Nick == "Anselm", "rename sets account and nick");
                File.WriteAllText(Path.Combine(a2.Dir, "General.con"), "broken");
                Bf2Profile r = Profiles.Repair(a2, "Anselm", docs);
                Check(r.Id == "0001" && r.Nick == "Anselm" && !File.Exists(Path.Combine(r.Dir, "General.con")) && Profiles.DefaultId() == "0001", "repair keeps id and name, fresh files");
                Check(Directory.Exists(Path.Combine(docs, "BF2_Backups")), "repair creates a backup");
                Profiles.Delete(r, docs);
                Check(!Directory.Exists(r.Dir) && Profiles.DefaultId() == "0002", "delete moves default to the remaining profile");
            }
            finally { Profiles.RootOverride = null; }
        }

        static void Radio()
        {
            string f = Path.Combine(_tmp, "AIRadioMessages.ai"), docs = Path.Combine(_tmp, "docs");
            File.WriteAllText(f, "rem voice name\r\naiComm.addAIRadioMessage \"medic\" 10 20 20\r\naiComm.addAIRadioMessage \"defend\" 10 10 1\r\n");
            Check(BotRadio.State(f) == true, "bot radio on initially");
            BotRadio.Set(f, false, docs);
            Check(BotRadio.State(f) == false && File.ReadAllText(f).StartsWith("rem voice name"), "bot radio off, other lines kept");
            BotRadio.Set(f, true, docs);
            Check(BotRadio.State(f) == true && File.ReadAllText(f).Contains("\r\naiComm.addAIRadioMessage \"medic\" 10 20 20"), "bot radio on again, line restored");
            Check(BotRadio.State(Path.Combine(_tmp, "missing.ai")) == null, "missing radio file");
        }

        static void ModDesc()
        {
            string g = Path.Combine(_tmp, "game"); string md = Path.Combine(g, "mods", "testmod"); Directory.CreateDirectory(md);
            File.WriteAllText(Path.Combine(md, "mod.desc"), "<?xml version=\"1.0\"?>\n<mod>\n <title>Test Mod</title>\n <version>2.5</version>\n</mod>\n");
            ModInfo mi = ModInfo.Read(g, "testmod");
            Check(mi.Title == "Test Mod" && mi.Version == "2.5", "mod.desc title and version");
            string xp = Path.Combine(g, "mods", "xpack"); Directory.CreateDirectory(xp);
            File.WriteAllText(Path.Combine(xp, "mod.desc"), "<mod><title>MODDESC_XP_TITLE</title><version>1.5</version></mod>");
            Check(ModInfo.Read(g, "xpack").Title == "xpack", "mod.desc translation key falls back to folder name");
            Check(GameLocator.VersionText(Path.Combine(g, "missing.exe")) == "missing", "version text for a missing exe");
        }

        static void Cmds()
        {
            List<CmdDef> all = Commands.All(null);
            CmdDef say = all.Find(delegate (CmdDef c) { return c.Cmd == "game.sayAll"; });
            Check(say != null && say.Build("Hi \"you\"") == "game.sayAll \"Hi 'you'\"", "sayAll quoting");
            CmdDef spawn = all.Find(delegate (CmdDef c) { return c.Cmd == "sv.spawnTime"; });
            Check(spawn != null && spawn.Build("10") == "sv.spawnTime 10", "setting command from table");
            bool allowed = true;
            foreach (CmdDef c in all) if (!(c.Cmd.StartsWith("sv.") || c.Cmd.StartsWith("mapList.") || c.Cmd.StartsWith("admin.") || c.Cmd.StartsWith("game.say") || c.Cmd.StartsWith("@"))) allowed = false;
            Check(allowed, "all commands pass the server whitelist");
        }

        static void Scrub()
        {
            string t = Diagnostics.Scrub("sv.password \"geheim\"\nserver 192.168.1.20 public 8.8.8.8\n");
            Check(!t.Contains("geheim") && t.Contains("192.168.1.20") && !t.Contains("8.8.8.8"), "diagnostics scrub password and public IP");
        }
    }
}
