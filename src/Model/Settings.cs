// Einstellungs-Tabelle (zweisprachig) - BF2 Manager. C# 5 (.NET Framework 4.x).
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
    public static class I18n
    {
        public static bool En;
        public static string T(string de, string en) { return En ? en : de; }
    }

    public enum SetType { Text, Bool, Int, FF, Minutes }

    public class SettingDef
    {
        public string Group, Key, LabelDe, LabelEn, Def, Unit, TipDe, TipEn;
        public SetType Type;
        public int Min, Max;
        public bool Live, Adv;
        public bool IsSpecial { get { return Key.StartsWith("@"); } }
        public string Label { get { return I18n.T(LabelDe, LabelEn); } }
        public string Tip { get { return I18n.T(TipDe, TipEn); } }
    }

    public class GroupDef
    {
        public string Id, De, En, Glyph; public bool Adv;
        public string Name { get { return I18n.T(De, En); } }
    }

    // ------------------------------------------------------------------ Einstellungen
    public static class Defs
    {
        public static readonly List<SettingDef> All = new List<SettingDef>();
        public static readonly List<GroupDef> Groups = new List<GroupDef>();

        const string GroupTable = @"
gen|Allgemein|General|0|Home
round|Runde|Round|0|Clock
rules|Spielregeln|Gameplay|0|Flag
ff|Friendly Fire|Friendly fire|0|Warn
timers|Wartezeiten|Timers|1|Clock
vote|Abstimmungen|Voting|1|Check
ranks|Ränge und Unlocks|Ranks and unlocks|1|Star
net|Netzwerk|Network|1|Globe
voip|Sprachchat|Voice chat|1|Mic
demo|Demos und Sponsor|Demos and sponsor|1|Video
misc|Funk und Admin|Radio and admin|1|Wrench
";

        // Gruppe|Schlüssel|DE|EN|Typ|Standard|Min|Max|Einheit|Live|Beschreibung DE|Beschreibung EN
        const string Table = @"
gen|sv.serverName|Servername|Server name|Text|BF2 Coop Server||||0|Name in der Serverliste (Join Local).|Name shown in the server list (Join Local).
gen|sv.password|Passwort|Password|Text|||||1|Leer lassen = kein Passwort. Wird beim Beitreten abgefragt.|Leave empty for no password. Players are asked for it when joining.
gen|sv.welcomeMessage|Willkommensnachricht|Welcome message|Text|||||1|Text, den Spieler beim Beitritt sehen.|Text players see when they join.
gen|sv.maxPlayers|Max. Spieler|Max. players|Int|64|1|64||0|Plätze auf dem Server inklusive Bots. Menschen und Bots müssen zusammen hineinpassen.|Server slots including bots. Humans and bots together must fit.
round|sv.timeLimit|Zeitlimit|Time limit|Minutes|0|0|600|min|1|0 = kein Zeitlimit. Die Datei speichert Sekunden, hier stehen Minuten.|0 = no time limit. The file stores seconds, shown here in minutes.
round|sv.scoreLimit|Punktelimit|Score limit|Int|0|0|10000||1|0 = kein Punktelimit.|0 = no score limit.
round|sv.ticketRatio|Tickets|Tickets|Int|100|10|500|%|1|Prozent der Standard-Tickets der Map. 200 = doppelt so viele.|Percent of the map's default tickets. 200 = twice as many.
round|sv.roundsPerMap|Runden pro Map|Rounds per map|Int|1|1|20||1|Wie oft eine Map hintereinander läuft, bevor die Rotation weitergeht.|How often a map is played in a row before the rotation moves on.
round|sv.numPlayersNeededToStart|Spieler zum Start|Players to start|Int|1|0|64||1|Ab so vielen Menschen beginnt die Runde. Für Coop ist 1 sinnvoll.|The round starts once this many humans are connected. 1 makes sense for coop.
rules|@commander|Commander erlauben|Allow commander|Bool|1||||1|Schaltet die Commander-Rolle für beide Teams an oder ab. Das Bot-Modul setzt das über host.sh_setEnableCommander, sofort bzw. zur nächsten Runde.|Enables or disables the commander role for both teams. The bot module applies it via host.sh_setEnableCommander, immediately or at the next round.
rules|sv.spawnTime|Respawn-Zeit|Respawn time|Int|15|0|300|s|1|Wartezeit bis zum Wiedereinstieg nach dem Tod.|Waiting time before respawning after death.
rules|sv.manDownTime|Wiederbelebungszeit|Revive time|Int|15|0|300|s|1|So lange kann ein Sanitäter einen Gefallenen wiederbeleben.|How long a medic can revive a downed soldier.
rules|sv.hitIndicator|Treffer-Anzeige|Hit indicator|Bool|1||||1|Kurzes Signal am Fadenkreuz bei einem Treffer.|Short crosshair signal when you hit someone.
rules|sv.noVehicles|Keine Fahrzeuge|No vehicles|Bool|0||||0|Reine Infanterie-Runden.|Infantry-only rounds.
rules|sv.autoBalanceTeam|Teams ausgleichen|Auto-balance teams|Bool|0||||1|Im Coop meist aus: Die Bots werden über die Bot-Verteilung verteilt, Menschen wählen ihr Team frei.|Usually off in coop: bots are distributed by the bot settings, humans pick their team freely.
rules|sv.teamRatioPercent|Teamverhältnis|Team ratio|Int|100|0|100|%|1|Erlaubter Größenunterschied der Teams, wenn Teams ausgleichen aktiv ist.|Allowed team size difference when auto-balance is on.
rules|sv.allowFreeCam|Freie Kamera|Free camera|Bool|0||||0|Freie Kamera beim Warten auf den Respawn.|Free camera while waiting to respawn.
rules|sv.allowExternalViews|Außenansicht|External views|Bool|1||||0|Dritte-Person-Ansicht in Fahrzeugen.|Third-person view in vehicles.
rules|sv.allowNoseCam|Nasenkamera|Nose camera|Bool|1||||0|Ansicht von der Flugzeugnase.|View from the aircraft nose.
ff|sv.soldierFriendlyFire|Soldaten|Soldiers|FF|1||||1|Schaden an eigenen Soldaten. An = 100 %, aus = 0 %.|Damage to own soldiers. On = 100 %, off = 0 %.
ff|sv.vehicleFriendlyFire|Fahrzeuge|Vehicles|FF|1||||1|Schaden an eigenen Fahrzeugen. An = 100 %, aus = 0 %.|Damage to own vehicles. On = 100 %, off = 0 %.
ff|sv.soldierSplashFriendlyFire|Explosionen an Soldaten|Splash on soldiers|FF|1||||1|Splash-Schaden von Granaten, Artillerie und Raketen an eigenen Soldaten.|Splash damage from grenades, artillery and rockets to own soldiers.
ff|sv.vehicleSplashFriendlyFire|Explosionen an Fahrzeugen|Splash on vehicles|FF|1||||1|Splash-Schaden an eigenen Fahrzeugen.|Splash damage to own vehicles.
ff|sv.friendlyFireWithMines|Minen treffen Eigene|Mines hurt own team|Bool|0||||1|Eigene Minen beschädigen das eigene Team.|Own mines damage your own team.
ff|sv.tkPunishEnabled|Teamkill-Bestrafung|Teamkill punish|Bool|1||||1|Das Opfer eines Teamkills darf den Täter bestrafen.|The victim of a teamkill may punish the killer.
ff|sv.tkPunishByDefault|Automatisch bestrafen|Punish automatically|Bool|0||||1|Teamkills werden ohne Rückfrage bestraft.|Teamkills are punished without asking.
ff|sv.tkNumPunishToKick|Bestrafungen bis Kick|Punishes until kick|Int|3|1|20||1|Nach so vielen Bestrafungen wird der Spieler gekickt.|A player is kicked after this many punishments.
timers|sv.startDelay|Startverzögerung|Start delay|Int|15|0|600|s|1|Wartezeit vor Rundenbeginn.|Delay before the round starts.
timers|sv.endDelay|Endverzögerung|End delay|Int|15|0|600|s|1|Wartezeit, bevor die Rundenstatistik erscheint.|Delay before the round statistics appear.
timers|sv.endOfRoundDelay|Pause nach Rundenende|End of round delay|Int|15|0|600|s|1|Zeit, bis die nächste Map geladen wird.|Time until the next map is loaded.
timers|sv.notEnoughPlayersRestartDelay|Neustart bei zu wenig Spielern|Restart if too few players|Int|15|0|600|s|1|Wartezeit, bevor bei zu wenigen Spielern neu gestartet wird.|Delay before restarting when there are not enough players.
timers|sv.timeBeforeRestartMap|Wartezeit vor Map-Neustart|Delay before map restart|Int|30|0|600|s|1|Countdown, wenn eine Map neu gestartet wird.|Countdown when a map is restarted.
vote|sv.votingEnabled|Abstimmungen|Voting|Bool|1||||1|Spieler dürfen über Kick oder Mapwechsel abstimmen.|Players may vote to kick or change the map.
vote|sv.voteTime|Abstimmungsdauer|Vote time|Int|90|10|600|s|1|So lange läuft eine Abstimmung.|How long a vote runs.
vote|sv.minPlayersForVoting|Mindestspieler|Minimum players|Int|2|1|64||1|Abstimmungen erst ab so vielen Menschen.|Votes only start with at least this many humans.
vote|sv.teamVoteOnly|Nur Team-Abstimmungen|Team votes only|Bool|1||||1|Nur das eigene Team stimmt ab.|Only your own team votes.
ranks|sv.useGlobalUnlocks|Globale Unlocks|Global unlocks|Bool|1||||0|Übernimmt online verdiente Unlocks. Für alle Waffen im LAN den Unlock-Patch unter Extras nutzen.|Uses unlocks earned online. For all weapons on LAN use the unlock patch under Extras.
ranks|sv.useGlobalRank|Globaler Rang|Global rank|Bool|0||||0|Ohne Online-Dienst wirkungslos.|Has no effect without an online service.
net|sv.serverPort|Server-Port|Server port|Int|16567|1024|65535||0|Port, über den Spieler beitreten. Standard 16567.|Port players join on. Default 16567.
net|sv.internet|Im Internet listen|List on the internet|Bool|0||||0|Aus = nur LAN.|Off = LAN only.
net|sv.serverIP|Server-IP|Server IP|Text|||||0|Leer = automatisch.|Empty = automatic.
net|sv.interfaceIP|Interface-IP|Interface IP|Text|||||0|Leer = alle Netzwerkkarten.|Empty = all network adapters.
net|sv.gameSpyPort|GameSpy-Port|GameSpy port|Int|29900|1024|65535||0|Abfrage-Port für Serverlisten.|Query port for server lists.
net|sv.allowNATNegotiation|NAT-Verhandlung|NAT negotiation|Bool|0||||0|Nur für Internet-Server relevant.|Only relevant for internet servers.
net|sv.bandwidthChoke|Bandbreitenlimit|Bandwidth limit|Int|0|0|100000||0|Begrenzt die Upload-Bandbreite des Servers. 0 = keine Begrenzung.|Limits the server upload bandwidth. 0 = unlimited.
net|sv.punkBuster|PunkBuster|PunkBuster|Bool|0||||0|Anti-Cheat, im LAN nicht nötig.|Anti-cheat, not needed on LAN.
voip|sv.voipEnabled|Sprachchat|Voice chat|Bool|1||||0|Eingebauter Team-Sprachchat.|Built-in team voice chat.
voip|sv.voipQuality|Sprachqualität|Voice quality|Int|3|1|10||0|Höher = bessere Qualität, mehr Bandbreite.|Higher = better quality, more bandwidth.
voip|sv.voipServerRemote|Externer Sprachserver|Remote voice server|Bool|0||||0|Sprachserver läuft auf einem anderen Rechner.|Voice server runs on another machine.
voip|sv.voipServerRemoteIP|Sprachserver-IP|Voice server IP|Text|||||0|Nur bei externem Sprachserver.|Only for a remote voice server.
voip|sv.voipServerPort|Sprachserver-Port|Voice server port|Int|55125|1024|65535||0||
voip|sv.voipBFClientPort|Sprach-Client-Port|Voice client port|Int|55123|1024|65535||0||
voip|sv.voipBFServerPort|Sprach-BF-Port|Voice BF port|Int|55124|1024|65535||0||
voip|sv.voipSharedPassword|Sprach-Passwort|Voice password|Text|||||0||
demo|sv.autoRecord|Demos aufzeichnen|Record demos|Bool|0||||0|Zeichnet jede Runde als Demo auf.|Records every round as a demo.
demo|sv.demoQuality|Demo-Qualität|Demo quality|Int|1|0|10||0||
demo|sv.demoIndexURL|Demo-Index-URL|Demo index URL|Text|http://||||0||
demo|sv.demoDownloadURL|Demo-Download-URL|Demo download URL|Text|http://||||0||
demo|sv.autoDemoHook|Demo-Hook|Demo hook|Text|adminutils/demo/rotate_demo.exe||||0||
demo|sv.sponsorText|Sponsor-Text|Sponsor text|Text|||||0|Text im Ladebildschirm.|Text on the loading screen.
demo|sv.sponsorLogoURL|Sponsor-Logo-URL|Sponsor logo URL|Text|||||0||
demo|sv.communityLogoURL|Community-Logo-URL|Community logo URL|Text|||||0||
misc|sv.radioSpamInterval|Funk-Spam-Intervall|Radio spam interval|Int|6|0|60|s|0|Zeitfenster, in dem Funksprüche gezählt werden. Gilt für Spieler; ob Bots betroffen sind, ist nicht dokumentiert.|Time window in which radio messages are counted. Applies to players; whether bots are affected is not documented.
misc|sv.radioMaxSpamFlagCount|Funk-Spam-Grenze|Radio spam limit|Int|6|0|60||0|So viele Funksprüche im Zeitfenster, dann wird gesperrt.|This many radio messages within the window lead to a block.
misc|sv.radioBlockedDurationTime|Funk-Sperrdauer|Radio block time|Int|30|0|600|s|0|So lange ist der Funk nach Spam gesperrt.|How long radio is blocked after spamming.
misc|sv.adminScript|Admin-Skript|Admin script|Text|default||||0|Python-Modul im Ordner admin. Nicht ändern, wenn unklar.|Python module in the admin folder. Do not change unless you know why.
";

        static Defs()
        {
            foreach (string raw in GroupTable.Split('\n'))
            {
                string[] p = raw.Trim().Split('|');
                if (p.Length < 5) continue;
                GroupDef g = new GroupDef(); g.Id = p[0]; g.De = p[1]; g.En = p[2]; g.Adv = p[3] == "1"; g.Glyph = G.ByName(p[4]);
                Groups.Add(g);
            }
            foreach (string raw in Table.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                string[] p = line.Split('|');
                if (p.Length < 12) continue;
                SettingDef d = new SettingDef();
                d.Group = p[0]; d.Key = p[1]; d.LabelDe = p[2]; d.LabelEn = p[3];
                d.Type = (SetType)Enum.Parse(typeof(SetType), p[4]);
                d.Def = p[5];
                d.Min = p[6].Length > 0 ? int.Parse(p[6]) : 0;
                d.Max = p[7].Length > 0 ? int.Parse(p[7]) : 0;
                d.Unit = p[8]; d.Live = p[9] == "1"; d.TipDe = p[10]; d.TipEn = p[11];
                GroupDef gd = GroupOf(d.Group); d.Adv = gd != null && gd.Adv;
                All.Add(d);
            }
        }

        public static GroupDef GroupOf(string id) { foreach (GroupDef g in Groups) if (g.Id == id) return g; return null; }

        public static SettingDef Get(string key)
        {
            foreach (SettingDef d in All) if (string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase)) return d;
            return null;
        }

        public static Dictionary<string, string> Defaults()
        {
            Dictionary<string, string> v = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (SettingDef d in All) v[d.Key] = d.Def;
            return v;
        }

        public static string FromCon(SettingDef d, string raw)
        {
            raw = (raw ?? "").Trim();
            switch (d.Type)
            {
                case SetType.Bool: return (raw == "1" || raw.ToLowerInvariant() == "true") ? "1" : "0";
                case SetType.FF: return Util.ToInt(raw, 0) > 0 ? "1" : "0";
                case SetType.Minutes: return ((int)Math.Round(Util.ToInt(raw, 0) / 60.0)).ToString();
                case SetType.Int: return Util.Clamp(Util.ToInt(raw, Util.ToInt(d.Def, 0)), d.Min, d.Max).ToString();
                default: return raw;
            }
        }

        public static string ToCon(SettingDef d, string ui)
        {
            switch (d.Type)
            {
                case SetType.Bool: return ui == "1" ? "1" : "0";
                case SetType.FF: return ui == "1" ? "100" : "0";
                case SetType.Minutes: return (Util.ToInt(ui, 0) * 60).ToString();
                case SetType.Int: return Util.ToInt(ui, 0).ToString();
                default: return "\"" + (ui ?? "").Replace("\"", "'") + "\"";
            }
        }

        public static string DefaultText(SettingDef d)
        {
            switch (d.Type)
            {
                case SetType.Bool: return d.Def == "1" ? I18n.T("an", "on") : I18n.T("aus", "off");
                case SetType.FF: return d.Def == "1" ? I18n.T("an (100 %)", "on (100 %)") : I18n.T("aus (0 %)", "off (0 %)");
                case SetType.Int:
                case SetType.Minutes: return d.Def + (d.Unit.Length > 0 ? " " + d.Unit : "") + (d.Def == "0" && (d.Key == "sv.timeLimit" || d.Key == "sv.scoreLimit" || d.Key == "sv.bandwidthChoke") ? I18n.T(" (kein Limit)", " (no limit)") : "");
                default: return d.Def.Length == 0 ? I18n.T("(leer)", "(empty)") : d.Def;
            }
        }

        public static bool Same(SettingDef d, string a, string b)
        {
            if (d.Type == SetType.Text) return (a ?? "") == (b ?? "");
            return Util.ToInt(a, 0) == Util.ToInt(b, 0);
        }
    }
}
