using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace MynaSkat.Core
{
    public enum CardValue { Digit7 = 0, Digit8 = 1, Digit9 = 2, Digit10 = 3, Jack = 4, Queen = 5, King = 6, Ace = 7 };

    public enum CardColor { Diamonds = 0, Hearts = 1, Spades = 2, Clubs = 3 };

    public class Card
    {
        public int InternalNumber { get; set; }

        public CardValue Value { get; set; }

        public CardColor Color { get; set; }

        private Card(int nr)
        {
            if (nr < 0 || nr > 31) throw new ArgumentException("Invalid internal card number");
            int c = nr / 8;
            int v = nr - c * 8;
            InternalNumber = nr;
            Value = (CardValue)Enum.Parse(typeof(CardValue), v.ToString());
            Color = (CardColor)Enum.Parse(typeof(CardColor), c.ToString());
        }

        public static List<Card> GenerateDeck()
        {
            var deck = new List<Card>();
            for (int nr = 0; nr < 32; nr++)
            {
                deck.Add(new Card(nr));
            }
            return deck;
        }

        public static string GetValueText(CardValue value)
        {
            switch (value)
            {
                case CardValue.Digit7:
                    return "7";
                case CardValue.Digit8:
                    return "8";
                case CardValue.Digit9:
                    return "9";
                case CardValue.Digit10:
                    return "10";
                case CardValue.Jack:
                    return "Bube";
                case CardValue.Queen:
                    return "Dame";
                case CardValue.King:
                    return "König";
                case CardValue.Ace:
                    return "Ass";
                default:
                    break;
            }
            return "";
        }

        public static string GetColorText(CardColor color)
        {
            switch (color)
            {
                case CardColor.Clubs:
                    return "Kreuz";
                case CardColor.Spades:
                    return "Pik";
                case CardColor.Hearts:
                    return "Herz";
                case CardColor.Diamonds:
                    return "Karo";
                default:
                    break;
            }
            return "";
        }

        public static List<Card> Draw(RNGCryptoServiceProvider rng, List<Card> deck, int count)
        {
            var ret = new List<Card>();
            for (; count > 0; count--)
            {
                ret.Add(DrawOne(rng, deck));
            }
            return ret;
        }

        public int GetOrderNumber(Game game)
        {
            var orderNumber = InternalNumber;
            if (game.Type == GameType.Grand ||
                game.Type == GameType.Color && !game.Color.HasValue)
            {
                if (Value == CardValue.Jack)
                {
                    orderNumber += 64;
                }
                else if (Value == CardValue.Digit10)
                {
                    orderNumber += 3;
                }
                else if (Value == CardValue.Queen || Value == CardValue.King)
                {
                    orderNumber -= 1;
                }
            }
            else if (game.Type == GameType.Color && game.Color.HasValue)
            {
                if (Color == game.Color && Value != CardValue.Jack)
                {
                    orderNumber += 32;
                }
                if (Value == CardValue.Jack)
                {
                    orderNumber += 64;
                }
                else if (Value == CardValue.Digit10)
                {
                    orderNumber += 3;
                }
                else if (Value == CardValue.Queen || Value == CardValue.King)
                {
                    orderNumber -= 1;
                }
            }
            return orderNumber;
        }

        public int Score
        {
            get
            {
                switch (Value)
                {
                    case CardValue.Jack:
                        return 2;
                    case CardValue.Queen:
                        return 3;
                    case CardValue.King:
                        return 4;
                    case CardValue.Digit10:
                        return 10;
                    case CardValue.Ace:
                        return 11;
                    default:
                        break;
                }
                return 0;
            }
        }

        public static int GetScore(List<Card> stitches, List<Card> skat)
        {
            var score = stitches.Sum(c => c.Score);
            if (skat != null)
            {
                score += skat.Sum(c => c.Score);
            }
            return score;
        }

        public static Card DrawOne(RNGCryptoServiceProvider rng, List<Card> deck)
        {
            var nr = Next(rng, deck.Count);
            var card = deck[nr];
            deck.RemoveAt(nr);
            return card;
        }

        private static int Next(RNGCryptoServiceProvider rng, int limit)
        {
            if (limit <= 0)
            {
                throw new ArgumentException($"Invalid upper limit {limit}.");
            }
            if (limit == 1)
            {
                return 0;
            }
            return (int)(Next(rng) % (uint)limit);
        }

        private static uint Next(RNGCryptoServiceProvider rng)
        {
            byte[] randomNumber = new byte[4];
            rng.GetBytes(randomNumber);
            return BitConverter.ToUInt32(randomNumber, 0);
        }

        public override bool Equals(object obj)
        {
            var c = obj as Card;
            if (c != null)
            {
                return c.InternalNumber == InternalNumber;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return InternalNumber;
        }

        public override string ToString()
        {
            return $"{GetColorText(Color)} {GetValueText(Value)}";
        }
    }
}
