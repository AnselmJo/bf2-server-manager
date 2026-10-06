// Commands.cs - Befehlskatalog für die Live-Konsole (Assistent und Hilfe) - BF2 Manager. C# 5.
using System;
using System.Collections.Generic;

namespace BF2Coop
{
    public class CmdDef
    {
        public string Cat, Cmd, Template, ParamDe, ParamEn, De, En, Default;
        public bool Danger, Quote;
        public string Desc { get { return I18n.T(De, En); } }
        public string Param { get { return ParamDe == null ? null : I18n.T(ParamDe, ParamEn); } }
        public bool HasParam { get { return Template.Contains("{x}"); } }

        // Fertiger Befehl mit eingesetztem Wert
        public string Build(string value)
        {
            if (!HasParam) return Template;
            string v = (value ?? "").Trim();
            if (Quote)
            {
                if (v.Length >= 2 && v.StartsWith("\"") && v.EndsWith("\"")) v = v.Substring(1, v.Length - 2);
                v = "\"" + v.Replace("\"", "'") + "\"";
            }
            return Template.Replace("{x}", v);
        }
    }

    public static class Commands
    {
        public static readonly string[] Cats = { "game", "players", "maps", "settings" };
        public static string CatName(string c)
        {
            switch (c)
            {
                case "game": return I18n.T("Spiel", "Game");
                case "players": return I18n.T("Spieler", "Players");
                case "maps": return "Maps";
                default: return I18n.T("Einstellungen", "Settings");
            }
        }

        static CmdDef C(string cat, string cmd, string tpl, string de, string en, string pDe, string pEn, string def)
        {
            CmdDef d = new CmdDef(); d.Cat = cat; d.Cmd = cmd; d.Template = tpl; d.De = de; d.En = en; d.ParamDe = pDe; d.ParamEn = pEn; d.Default = def;
            return d;
        }

        // vals: aktuelle Werte der Servereinstellungen (Schlüssel wie in Defs), für Vorschläge
        public static List<CmdDef> All(IDictionary<string, string> vals)
        {
            List<CmdDef> r = new List<CmdDef>();
            CmdDef d;
            r.Add(C("game", "@nextmap", "@nextmap", "Nächste Map sofort starten. Setzt vorher die Bot-Werte der nächsten Map (empfohlen statt admin.runNextLevel).", "Start the next map now. Applies the bot values of the next map first (recommended instead of admin.runNextLevel).", null, null, null));
            r.Add(C("game", "admin.restartMap", "admin.restartMap", "Aktuelle Runde neu starten. Die Botzahl bleibt gleich.", "Restart the current round. The bot count stays the same.", null, null, null));
            d = C("game", "game.sayAll", "game.sayAll {x}", "Nachricht an alle Spieler (erscheint im Chat).", "Message to all players (appears in chat).", "Text der Nachricht", "Message text", I18n.T("Hallo zusammen!", "Hello everyone!")); d.Quote = true; r.Add(d);
            r.Add(C("game", "@commander", "@commander {x}", "Commander-Rolle für beide Teams erlauben (1) oder sperren (0).", "Allow (1) or block (0) the commander role for both teams.", "1 = erlaubt, 0 = gesperrt", "1 = allowed, 0 = blocked", "1"));
            r.Add(C("game", "admin.nextLevel", "admin.nextLevel {x}", "Legt fest, welche Map aus der Rotation als nächste kommt (Zählung ab 0).", "Sets which map of the rotation comes next (counting from 0).", "Nummer in der Rotation, ab 0", "Index in the rotation, from 0", "0"));
            r.Add(C("game", "admin.runNextLevel", "admin.runNextLevel", "Wechselt sofort zur nächsten Map – ohne die Bot-Werte anzupassen.", "Switches to the next map immediately – without adjusting bot values.", null, null, null));
            r.Add(C("game", "admin.currentLevel", "admin.currentLevel", "Zeigt die Nummer der aktuellen Map (Ergebnis im Log).", "Shows the index of the current map (result in the log).", null, null, null));

            r.Add(C("players", "admin.listPlayers", "admin.listPlayers", "Listet alle Spieler mit ID (Ergebnis im Log). Die ID braucht man für Kick und Bann.", "Lists all players with ID (result in the log). The ID is needed for kick and ban.", null, null, null));
            d = C("players", "admin.kickPlayer", "admin.kickPlayer {x}", "Wirft einen Spieler vom Server. Einfacher: Live › Spieler › Kick.", "Kicks a player from the server. Easier: Live › Players › Kick.", "Spieler-ID (siehe admin.listPlayers)", "Player ID (see admin.listPlayers)", ""); d.Danger = true; r.Add(d);
            d = C("players", "admin.banPlayer", "admin.banPlayer {x}", "Sperrt einen Spieler dauerhaft (über seine Adresse).", "Bans a player permanently (by address).", "Spieler-ID (siehe admin.listPlayers)", "Player ID (see admin.listPlayers)", ""); d.Danger = true; r.Add(d);
            r.Add(C("players", "admin.listBannedAddresses", "admin.listBannedAddresses", "Zeigt alle gesperrten Adressen (Ergebnis im Log).", "Shows all banned addresses (result in the log).", null, null, null));
            d = C("players", "admin.clearBanList", "admin.clearBanList", "Hebt alle Sperren auf.", "Removes all bans.", null, null, null); d.Danger = true; r.Add(d);

            r.Add(C("maps", "mapList.list", "mapList.list", "Zeigt die Rotation des laufenden Servers (Ergebnis im Log).", "Shows the rotation of the running server (result in the log).", null, null, null));
            r.Add(C("maps", "mapList.currentMap", "mapList.currentMap", "Nummer der laufenden Map in der Rotation.", "Index of the running map in the rotation.", null, null, null));
            r.Add(C("maps", "mapList.mapCount", "mapList.mapCount", "Anzahl der Maps in der Rotation.", "Number of maps in the rotation.", null, null, null));
            r.Add(C("maps", "mapList.append", "mapList.append {x}", "Hängt eine Map hinten an die Rotation an (nur bis zum Neustart; dauerhaft über die Seite Maps).", "Appends a map to the rotation (until restart; permanently via the Maps page).", "Map Modus Größe", "map mode size", "strike_at_karkand gpm_coop 32"));
            d = C("maps", "mapList.remove", "mapList.remove {x}", "Entfernt eine Map aus der Rotation (nur bis zum Neustart).", "Removes a map from the rotation (until restart).", "Nummer in der Rotation, ab 0", "Index in the rotation, from 0", "0"); d.Danger = true; r.Add(d);
            d = C("maps", "mapList.clear", "mapList.clear", "Leert die Rotation des laufenden Servers. Danach Maps anhängen oder „Rotation übernehmen“.", "Clears the rotation of the running server. Then append maps or use “Apply rotation”.", null, null, null); d.Danger = true; r.Add(d);

            foreach (SettingDef s in Defs.All)
            {
                if (!s.Live || s.IsSpecial) continue;
                string pDe, pEn, cur = null;
                string v; if (vals != null && vals.TryGetValue(s.Key, out v)) cur = Defs.ToCon(s, v).Trim('"');
                switch (s.Type)
                {
                    case SetType.Bool: pDe = "0 = aus, 1 = an"; pEn = "0 = off, 1 = on"; break;
                    case SetType.FF: pDe = "0 = aus, 100 = an (Prozent)"; pEn = "0 = off, 100 = on (percent)"; break;
                    case SetType.Minutes: pDe = "Sekunden (Minuten × 60), 0 = kein Limit"; pEn = "seconds (minutes × 60), 0 = no limit"; break;
                    case SetType.Int: pDe = s.Min + "–" + s.Max + (s.Unit.Length > 0 ? " " + s.Unit : ""); pEn = pDe; break;
                    default: pDe = "Text"; pEn = "text"; break;
                }
                d = C("settings", s.Key, s.Key + " {x}", s.LabelDe + ": " + s.TipDe, s.LabelEn + ": " + s.TipEn, pDe, pEn, cur ?? Defs.ToCon(s, s.Def).Trim('"'));
                d.Quote = s.Type == SetType.Text;
                r.Add(d);
            }
            return r;
        }

        // Für die Autovervollständigung im Konsolenfeld
        public static string[] Names(IDictionary<string, string> vals)
        {
            List<string> r = new List<string>();
            foreach (CmdDef c in All(vals)) r.Add(c.HasParam ? c.Template.Replace("{x}", "").TrimEnd() + " " : c.Template);
            return r.ToArray();
        }
    }
}
