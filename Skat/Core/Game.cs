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

        public string GetGameText()
        {
            if (Type == GameType.Null)
            {
                return "Null";
            }
            return Type == GameType.Grand ? "Grand" : Card.GetColorText(Color.Value);
        }

        public string GetGameAndOptionText()
        {
            string text = GetGameText();
            if (Type == GameType.Grand || Type == GameType.Color)
            {
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
                    if (Option.HasFlag(GameOption.Schneider))
                    {
                        text += " Schneider Angesagt";
                    }
                    if (Option.HasFlag(GameOption.Schwarz))
                    {
                        text += " Schwarz Angesagt";
                    }
                }
            }
            else if (Type == GameType.Null)
            {
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
                return stitches.Count == 0; // no stitches
            }
            if (Option.HasFlag(GameOption.Ouvert) ||
                Option.HasFlag(GameOption.Schwarz))
            {
                return stitches.Count == 30; // all stitches (10 x 3 cards)
            }
            var score = Card.GetScore(stitches, skat);
            if (Option.HasFlag(GameOption.Schneider))
            {
                return score >= 90;
            }
            return score >= 61;
        }

        public GameValue GetGameValue(MatadorsJackStraight spitzen, List<Card> stitches, List<Card> skat, int bidValue)
        {
            var score = 0;
            bool schneider = false;
            bool schwarz = false;
            bool gamePlayerSchneider = false;
            bool gamePlayerSchwarz = false;
            if (Type != GameType.Null)
            {
                if (stitches.Count == 0)
                {
                    gamePlayerSchwarz = true;
                }
                score = Card.GetScore(stitches, skat);
                gamePlayerSchneider = score <= 30;
                schneider = score >= 90;
                schwarz = stitches.Count == 30;
            }
            var gameValue = new GameValue();
            // check if bid value is exceeded considering schneider and schwarz
            var gameBidValue = GetBidValue(spitzen, schneider, schwarz);
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
                gameValue.BidExceeded = true;
                gameValue.IsWinner = false;
                gameValue.Score *= -2;
                gameValue.Description = $"Das Spiel wurde überreizt mit {bidValue}. {GetGameAndOptionText()} : {calc} x -2 = {gameValue.Score}.";
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
                    game = GetGameAndOptionText();
                }
                else
                {
                    var with = spitzen.With ? "Mit" : "Ohne";
                    game = $"{with} {spitzen.Count} spielt {spitzen.Play} ";
                    factor = spitzen.Play;
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
                    if (schneider || gamePlayerSchneider)
                    {
                        factor++;
                        game += $"Schneider {factor} ";
                    }
                    if (Option.HasFlag(GameOption.Schneider))
                    {
                        factor++;
                        if (!schneider && !gamePlayerSchneider)
                        {
                            game += "Schneider ";
                        }
                        game += $"Angesagt {factor} ";
                    }
                    if (schwarz || gamePlayerSchwarz)
                    {
                        factor++;
                        game += $"Schwarz {factor} ";
                    }
                    if (Option.HasFlag(GameOption.Schwarz))
                    {
                        factor++;
                        if (!schwarz && !gamePlayerSchwarz)
                        {
                            game += "Schwarz ";
                        }
                        game += $"Angesagt {factor} ";
                    }
                    game += $"{GetGameText()} ";
                    baseValue = GetGrandOrColorBaseValue();
                }
                gameValue.Score = factor * baseValue;
                calc = $"{factor} x {baseValue}";
                if (!IsWinner(stitches, skat))
                {
                    gameValue.Score *= -2;
                    gameValue.IsWinner = false;
                    gameValue.Description = $"{game}: {calc} x -2 = {gameValue.Score}.";
                }
                else
                {
                    gameValue.Description = $"{game}: {calc} = {gameValue.Score}.";
                }
            }
            return gameValue;
        }

        public int GetBidValue(MatadorsJackStraight jackStraight, bool schneider = false, bool schwarz = false)
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
            if (schneider)
            {
                mult++;
            }
            if (schwarz)
            {
                mult++;
            }
            return mult * GetGrandOrColorBaseValue();
        }

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
            return $"{GetGameAndOptionText()}";
        }
    }
}
