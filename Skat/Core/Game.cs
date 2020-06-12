using System;
using System.Collections.Generic;
using System.Linq;

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

        public MatadorsJackStraight GetMatadorsJackStraight(List<Card> playerCards, List<Card> skat)
        {
            if (Type == GameType.Null) return new MatadorsJackStraight();
            var cards = new List<Card>();
            cards.AddRange(playerCards);
            if (skat != null)
            {
                cards.AddRange(skat);
            }
            bool with = cards.Any((c) => c.Value == CardValue.Jack && c.Color == CardColor.Clubs);
            int value = 1;
            foreach (var col in new[] { CardColor.Spades, CardColor.Hearts, CardColor.Diamonds })
            {
                var hasBube = cards.Any((c) => c.Value == CardValue.Jack && c.Color == col);
                if (with && !hasBube || !with && hasBube)
                {
                    break;
                }
                value += 1;
            }
            if (value == 4 && Type == GameType.Color)
            {
                foreach (var v in new[] { CardValue.Ace, CardValue.Digit10, CardValue.King, CardValue.Queen, CardValue.Digit9, CardValue.Digit8, CardValue.Digit7 })
                {
                    var hasValue = cards.Any((c) => c.Color == Color.Value && c.Value == v);
                    if (with && !hasValue || !with && hasValue)
                    {
                        break;
                    }
                    value += 1;
                }
            }
            return new MatadorsJackStraight { With = with, Count = value };
        }

        public bool IsWinner(List<Card> stitches, List<Card> skat)
        {
            if (Type == GameType.Null)
            {
                return stitches.Count == 0;
            }
            if (Option.HasFlag(GameOption.Ouvert) ||
                Option.HasFlag(GameOption.Schwarz))
            {
                return stitches.Count == 30; // alle stiche bekommen, gegner keinen stich
            }
            // punkte über alle stiche und skat
            var score = Card.GetScore(stitches, skat);
            if (Option.HasFlag(GameOption.Hand))
            {
                return score >= 90;
            }
            return score >= 61;
        }

        public GameValue GetGameValue(MatadorsJackStraight spitzen, List<Card> stitches, List<Card> skat, int bidValue)
        {
            var gameValue = new GameValue();
            var gameBidValue = GetBidValue(spitzen);
            if (gameBidValue < bidValue)
            {
                int baseValue;
                if (Type == GameType.Null)
                {
                    baseValue = GetNullBaseValue();
                }
                else
                {
                    baseValue = GetGrandOrColorBaseValue();
                }
                gameValue.Score = baseValue;
                int factor = 1;
                while (gameValue.Score < bidValue)
                {
                    gameValue.Score += baseValue;
                    factor++;
                }
                var calc = factor == 1 ? $"{baseValue}" : $"{factor} x {baseValue}";
                string game;
                if (Type != GameType.Color)
                {
                    game = $"{Type}";
                }
                else
                {
                    game = $"{Color}";
                }
                gameValue.BidExceeded = true;
                gameValue.IsWinner = false;
                gameValue.Score *= -2;
                gameValue.Description = $"Überreizt mit {bidValue}! Verloren! {game} : {calc} x -2 = {gameValue.Score}.";
            }
            else
            {
                string calc;
                string game;
                int baseValue;
                int factor = 1;
                if (Type == GameType.Null)
                {
                    baseValue = GetNullBaseValue();
                    gameValue.Score = baseValue;
                    game = $"{Type} ";
                    if (Option != GameOption.None)
                    {
                        game += $" {Option}";
                    }
                }
                else
                {
                    var with = spitzen.With ? "Mit" : "Ohne";
                    game = $"{with} {spitzen.Count} spielt {spitzen.Play} ";
                    factor = spitzen.Play;
                    var score = Card.GetScore(stitches, skat);
                    bool schneider = score >= 90;
                    bool schwarz = stitches.Count == 30;
                    if (Option.HasFlag(GameOption.Hand))
                    {
                        factor++;
                        game += $"Hand {factor} ";
                    }
                    if (Option.HasFlag(GameOption.Ouvert))
                    {
                        factor++;
                        game += $"Ouvert {factor} ";
                    }
                    if (schneider)
                    {
                        factor++;
                        game += $"Schneider {factor} ";
                    }
                    if (Option.HasFlag(GameOption.Schneider))
                    {
                        factor++;
                        game += $"Angesagt {factor} ";
                    }
                    if (schwarz)
                    {
                        factor++;
                        game += $"Schwarz {factor} ";
                    }
                    if (Option.HasFlag(GameOption.Schwarz))
                    {
                        factor++;
                        game += $"Angesagt {factor} ";
                    }
                    if (Type != GameType.Color)
                    {
                        game += $"{Type} ";
                    }
                    else
                    {
                        game += $"{Color} ";
                    }
                    baseValue = GetGrandOrColorBaseValue();
                }
                gameValue.Score = factor * baseValue;
                calc = $"{factor} x {baseValue}";
                if (!IsWinner(stitches, skat))
                {
                    gameValue.Score *= -2;
                    gameValue.IsWinner = false;
                    gameValue.Description = $"Verloren! {game}: {calc} x -2 = {gameValue.Score}.";
                }
                else
                {
                    gameValue.Description = $"Gewonnen! {game}: {calc} = {gameValue.Score}.";
                }
            }
            return gameValue;
        }

        public int GetBidValue(MatadorsJackStraight jackStraight)
        {
            if (Type == GameType.Null)
            {
                return GetNullBaseValue();
            }
            int mult = jackStraight.Play;
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
            return mult * GetGrandOrColorBaseValue();
        }
        
        /// <summary>
        /// Returns the value for the Null game.
        /// </summary>
        /// <returns>game value</returns>
        public int GetNullBaseValue()
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
        /// Returns the base value for a Grand or Color game.
        /// </summary>
        /// <returns>game base value</returns>
        public int GetGrandOrColorBaseValue()
        {
            if (Type == GameType.Grand)
            {
                return 24;
            }
            if (Type == GameType.Color)
            {
                switch (Color)
                {
                    case CardColor.Clubs:
                        return 12;
                    case CardColor.Spades:
                        return 11;
                    case CardColor.Hearts:
                        return 10;
                    case CardColor.Diamonds:
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
