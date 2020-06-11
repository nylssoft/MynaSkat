using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MynaSkat.Core
{
    public enum GameType { Grand, Color, Null };

    [Flags]
    public enum GameOption { None = 0, Ouvert = 1, Hand = 2, Schneider = 4, Schwarz = 8 };

    public class Game
    {
        public GameType Type { get; set; } = GameType.Grand;

        public GameOption Option { get; set; } = GameOption.None;

        public CardColor? Color { get; set; } = null;

        public Game(GameType gameType, CardColor? gameColor = null)
        {
            Type = gameType;
            Color = gameColor;
        }

        /// <summary>
        /// Returns the game of the text with all options.
        /// </summary>
        /// <param name="schneider">true if schneider; false otherwise</param>
        /// <param name="schwarz">true if schwarz; false otherwise</param>
        /// <returns>game text</returns>
        public string GetGameText(bool schneider = false, bool schwarz = false)
        {
            string text = "";
            if (Type == GameType.Grand || Type == GameType.Color)
            {
                text = Type == GameType.Grand ? "Grand" : Color.ToString();
                if (Option.HasFlag(GameOption.Ouvert))
                {
                    text += " Ouvert"; // schneider schwarz angesagt
                }
                else
                {
                    if (Option.HasFlag(GameOption.Hand))
                    {
                        text += " Hand";
                    }
                    if (schneider)
                    {
                        text += " Schneider";
                    }
                    if (Option.HasFlag(GameOption.Schneider))
                    {
                        text += " Schneider Angesagt";
                    }
                    if (schwarz)
                    {
                        text += " Schwarz";
                    }
                    if (Option.HasFlag(GameOption.Schwarz))
                    {
                        text += " Schwarz Angesagt";
                    }
                }
            }
            else if (Type == GameType.Null)
            {
                text = "Null";
                if (Option.HasFlag(GameOption.Ouvert))
                {
                    text += " Ouvert";
                }
                if (Option.HasFlag(GameOption.Hand))
                {
                    text += " Hand";
                }
            }
            return text;
        }

        public Spitzen GetSpitzen(List<Card> playerCards, List<Card> skat)
        {
            if (Type == GameType.Null) return new Spitzen();
            var cards = new List<Card>();
            cards.AddRange(playerCards);
            if (skat != null)
            {
                cards.AddRange(skat);
            }
            bool with = cards.Any((c) => c.Value == CardValue.Bube && c.Color == CardColor.Kreuz);
            int value = 1;
            foreach (var col in new[] { CardColor.Pik, CardColor.Herz, CardColor.Karo })
            {
                var hasBube = cards.Any((c) => c.Value == CardValue.Bube && c.Color == col);
                if (with && !hasBube || !with && hasBube)
                {
                    break;
                }
                value += 1;
            }
            if (value == 4 && Type == GameType.Color)
            {
                foreach (var v in new[] { CardValue.Ass, CardValue.Ziffer10, CardValue.Koenig, CardValue.Dame, CardValue.Ziffer9, CardValue.Ziffer8, CardValue.Ziffer7 })
                {
                    var hasValue = cards.Any((c) => c.Color == Color.Value && c.Value == v);
                    if (with && !hasValue || !with && hasValue)
                    {
                        break;
                    }
                    value += 1;
                }
            }
            return new Spitzen { Mit = with, Anzahl = value };
        }

        public bool IsWinner(List<Card> stichList, List<Card> skat)
        {
            if (Type == GameType.Null)
            {
                return stichList.Count == 0;
            }
            if (Option.HasFlag(GameOption.Ouvert) ||
                Option.HasFlag(GameOption.Schwarz))
            {
                return stichList.Count == 30; // alle stiche bekommen, gegner keinen stich
            }
            // punkte über alle stiche und skat
            var augen = Card.GetAugen(stichList, skat);
            if (Option.HasFlag(GameOption.Hand))
            {
                return augen >= 90;
            }
            return augen >= 61;
        }

        public Spielwert GetSpielWert(Spitzen spitzen, List<Card> stichList, List<Card> skat, int reizWert)
        {
            var spielwert = new Spielwert();
            var gameReizValue = GetReizWert(spitzen);
            if (gameReizValue < reizWert)
            {
                int wert;
                if (Type == GameType.Null)
                {
                    wert = GetBestaendigerWert();
                }
                else
                {
                    wert = GetGrundWert();
                }
                spielwert.Punkte = wert;
                int mult = 1;
                while (spielwert.Punkte < reizWert)
                {
                    spielwert.Punkte += wert;
                    mult++;
                }
                var calc = mult == 1 ? $"{wert}" : $"{mult} x {wert}";
                string spiel;
                if (Type != GameType.Color)
                {
                    spiel = $"{Type}";
                }
                else
                {
                    spiel = $"{Color}";
                }
                spielwert.Ueberreizt = true;
                spielwert.Gewonnen = false;
                spielwert.Punkte *= -2;
                spielwert.Beschreibung = $"Überreizt mit {reizWert}! Verloren! {spiel} : {calc} x -2 = {spielwert.Punkte}.";
            }
            else
            {
                string calc;
                string spiel;
                int wert;
                int mult = 1;
                if (Type == GameType.Null)
                {
                    wert = GetBestaendigerWert();
                    spielwert.Punkte = wert;
                    spiel = $"{Type} ";
                    if (Option != GameOption.None)
                    {
                        spiel += $" {Option}";
                    }
                }
                else
                {
                    var mit = spitzen.Mit ? "Mit" : "Ohne";
                    spiel = $"{mit} {spitzen.Anzahl} spielt {spitzen.Spielt} ";
                    mult = spitzen.Spielt;
                    var augen = Card.GetAugen(stichList, skat);
                    bool schneider = augen >= 90;
                    bool schwarz = stichList.Count == 30;
                    if (Option.HasFlag(GameOption.Hand))
                    {
                        mult++;
                        spiel += $"Hand {mult} ";
                    }
                    if (Option.HasFlag(GameOption.Ouvert))
                    {
                        mult++;
                        spiel += $"Ouvert {mult} ";
                    }
                    if (schneider)
                    {
                        mult++;
                        spiel += $"Schneider {mult} ";
                    }
                    if (Option.HasFlag(GameOption.Schneider))
                    {
                        mult++;
                        spiel += $"Angesagt {mult} ";
                    }
                    if (schwarz)
                    {
                        mult++;
                        spiel += $"Schwarz {mult} ";
                    }
                    if (Option.HasFlag(GameOption.Schwarz))
                    {
                        mult++;
                        spiel += $"Angesagt {mult} ";
                    }
                    if (Type != GameType.Color)
                    {
                        spiel += $"{Type} ";
                    }
                    else
                    {
                        spiel += $"{Color} ";
                    }
                    wert = GetGrundWert();
                }
                spielwert.Punkte = mult * wert;
                calc = $"{mult} x {wert}";
                if (!IsWinner(stichList, skat))
                {
                    spielwert.Punkte *= -2;
                    spielwert.Gewonnen = false;
                    spielwert.Beschreibung = $"Verloren! {spiel}: {calc} x -2 = {spielwert.Punkte}.";
                }
                else
                {
                    spielwert.Beschreibung = $"Gewonnen! {spiel}: {calc} = {spielwert.Punkte}.";
                }
            }
            return spielwert;
        }

        public int GetReizWert(Spitzen spitzen)
        {
            if (Type == GameType.Null)
            {
                return GetBestaendigerWert();
            }
            int mult = spitzen.Spielt;
            if (Option.HasFlag(GameOption.Hand))
            {
                mult++;
            }
            if (Option.HasFlag(GameOption.Ouvert))
            {
                mult++;
            }
            if (Option.HasFlag(GameOption.Schneider))
            {
                mult++;
            }
            if (Option.HasFlag(GameOption.Schwarz))
            {
                mult++;
            }
            return mult * GetGrundWert();
        }
        
        /// <summary>
        /// Returns the value for the Null game.
        /// </summary>
        /// <returns>game value</returns>
        public int GetBestaendigerWert()
        {
            if (Type == GameType.Null)
            {
                if (Option.HasFlag(GameOption.Ouvert))
                {
                    if (Option.HasFlag(GameOption.Hand))
                    {
                        return 59;
                    }
                    return 46;
                }
                if (Option.HasFlag(GameOption.Hand))
                {
                    return 35;
                }
                return 23;
            }
            return 0;
        }

        /// <summary>
        /// Returns the value for a Grand or Color game.
        /// </summary>
        /// <returns>game value</returns>
        public int GetGrundWert()
        {
            if (Type == GameType.Grand)
            {
                return 24;
            }
            if (Type == GameType.Color)
            {
                switch (Color)
                {
                    case CardColor.Kreuz:
                        return 12;
                    case CardColor.Pik:
                        return 11;
                    case CardColor.Herz:
                        return 10;
                    case CardColor.Karo:
                        return 9;
                    default:
                        break;
                }
            }
            return 0;
        }

        public override bool Equals(object obj)
        {
            var g = obj as Game;
            if (g != null)
            {
                return g.Type == Type && g.Color == Color && g.Option == Option;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var ret = Type.GetHashCode();
            if (Color != null)
            {
                ret += Color.GetHashCode() * 27;
            }
            ret += (int)Option * 113;
            return ret;
        }

        public override string ToString()
        {
            string ret;
            if (Type != GameType.Color)
            {
                ret = $"{Type}";
            }
            else
            {
                ret = $"{Color}";
            }
            if (Option != GameOption.None)
            {
                ret += $" {Option}";
            }
            return ret;
        }
    }
}
