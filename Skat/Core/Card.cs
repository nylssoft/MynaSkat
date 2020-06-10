using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace MynaSkat.Core
{
    public enum CardValue { Ziffer7 = 0, Ziffer8 = 1, Ziffer9 = 2, Ziffer10 = 3, Bube = 4, Dame = 5, Koenig = 6, Ass = 7 };
    public enum CardColor { Karo = 0, Herz = 1, Pik = 2, Kreuz = 3 };

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
                if (Value == CardValue.Bube)
                {
                    orderNumber += 64;
                }
                else if (Value == CardValue.Ziffer10)
                {
                    orderNumber += 3;
                }
                else if (Value == CardValue.Dame || Value == CardValue.Koenig)
                {
                    orderNumber -= 1;
                }
            }
            else if (game.Type == GameType.Color && game.Color.HasValue)
            {
                if (Color == game.Color && Value != CardValue.Bube)
                {
                    orderNumber += 32;
                }
                if (Value == CardValue.Bube)
                {
                    orderNumber += 64;
                }
                else if (Value == CardValue.Ziffer10)
                {
                    orderNumber += 3;
                }
                else if (Value == CardValue.Dame || Value == CardValue.Koenig)
                {
                    orderNumber -= 1;
                }
            }
            return orderNumber;
        }

        public static void Sort(List<Card> cards, Game game)
        {
            cards.Sort((b, a) => a.GetOrderNumber(game).CompareTo(b.GetOrderNumber(game)));
        }

        public int Augen
        {
            get
            {
                switch (Value)
                {
                    case CardValue.Bube:
                        return 2;
                    case CardValue.Dame:
                        return 3;
                    case CardValue.Koenig:
                        return 4;
                    case CardValue.Ziffer10:
                        return 10;
                    case CardValue.Ass:
                        return 11;
                    default:
                        break;
                }
                return 0;
            }
        }

        public static int GetAugen(List<Card> stichList, List<Card> skat)
        {
            var augen = stichList.Sum(c => c.Augen);
            if (skat != null)
            {
                augen += skat.Sum(c => c.Augen);
            }
            return augen;
        }

        private static Card DrawOne(RNGCryptoServiceProvider rng, List<Card> deck)
        {
            var nr = Next(rng, deck.Count);
            var card = deck[nr];
            deck.RemoveAt(nr);
            return card;
        }

        private static int Next(RNGCryptoServiceProvider rng, int upper_limit)
        {
            if (upper_limit <= 0)
            {
                throw new ArgumentException($"Invalid upper limit {upper_limit}.");
            }
            if (upper_limit == 1)
            {
                return 0;
            }
            return (int)(Next(rng) % (uint)upper_limit);
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
            return $"{Color} {Value}";
        }
    }
}
