using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
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
                if (Option.HasFlag(GameOption.Hand))
                {
                    text += " Hand";
                }
                if (Option.HasFlag(GameOption.Ouvert))
                {
                    text += " Ouvert"; // schneider schwarz angesagt
                }
                else
                {
                    if (schneider)
                    {
                        text += " Schneider";
                    }
                    if (Option.HasFlag(GameOption.Schneider))
                    {
                        text += " Schneider angesagt";
                    }
                    if (schwarz)
                    {
                        text += " Schwarz";
                    }
                    if (Option.HasFlag(GameOption.Schwarz))
                    {
                        text += " Schwarz angesagt";
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
                return stichList.Count == 10; // alle stiche bekommen, gegner keiner stich
            }
            // punkte über alle stiche und skat
            var augen = Card.GetAugen(stichList, skat);
            if (Option.HasFlag(GameOption.Hand))
            {
                return augen >= 90;
            }
            return augen >= 61;
        }

        public int GetSpielWert(Spitzen spitzen, List<Card> stichList, List<Card> skat)
        {
            int spielwert;
            if (Type == GameType.Null)
            {
                spielwert = GetBestaendigerWert();
            }
            else
            {
                var mult = spitzen.Anzahl;
                var augen = Card.GetAugen(stichList, skat);
                bool schneider = augen >= 90;
                bool schwarz = stichList.Count == 10;
                mult += GetGewinnStufe(schneider, schwarz);
                spielwert = GetGrundWert() * mult;
            }
            if (!IsWinner(stichList, skat))
            {
                spielwert *= -2;
            }
            return spielwert;
        }

        public int GetReizValue(Spitzen spitzen)
        {
            if (Type == GameType.Null)
            {
                return GetBestaendigerWert();
            }
            return (spitzen.Anzahl + GetGewinnStufe()) * GetGrundWert();
        }

        /// <summary>
        /// Returns the factor for a game win.
        /// </summary>
        /// <param name="schneider">true if schneider;false otherwise</param>
        /// <param name="schwarz">true if schwarz; false otherwise</param>
        /// <returns>factor for game win</returns>
        public int GetGewinnStufe(bool schneider = false, bool schwarz = false)
        {
            int stufe = 1;
            if (Type == GameType.Grand || Type == GameType.Color)
            {
                if (Option.HasFlag(GameOption.Hand))
                {
                    stufe++;
                }
                if (schneider)
                {
                    stufe++;
                }
                if (Option.HasFlag(GameOption.Schneider))
                {
                    stufe++;
                }
                if (schwarz)
                {
                    stufe++;
                }
                if (Option.HasFlag(GameOption.Schwarz))
                {
                    stufe++;
                }
                if (Option.HasFlag(GameOption.Ouvert))
                {
                    stufe++;
                }
            }
            return stufe;
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
