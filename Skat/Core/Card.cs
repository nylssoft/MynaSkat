using System;
using System.Collections.Generic;
using System.Linq;
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

        public static int GetFactor(Game game, List<Card> cards)
        {            
            int fac = 1;
            bool hasKreuzBube = cards.Any((c) => c.Value == CardValue.Bube && c.Color == CardColor.Kreuz);
            bool hasPikBube = cards.Any((c) => c.Value == CardValue.Bube && c.Color == CardColor.Pik);
            bool hasHerzBube = cards.Any((c) => c.Value == CardValue.Bube && c.Color == CardColor.Herz);
            bool hasKaroBube = cards.Any((c) => c.Value == CardValue.Bube && c.Color == CardColor.Karo);
            if (hasKreuzBube)
            {
                fac += 1; // mit 1 spielt...
                if (hasPikBube)
                {
                    fac += 1;
                    if (hasHerzBube)
                    {
                        fac += 1;
                        if (hasKaroBube)
                        {
                            fac += 1;
                            // @TODO: mit 5 spielt 6, mit 6 spielt 7...?
                            // geht viel besser...
                        }
                    }
                }
            }
            else
            {
                fac += 1; // ohne 1 spielt 2
                if (!hasPikBube)
                {
                    fac += 1; // spielt 3
                    if (!hasHerzBube)
                    {
                        fac += 1; // spielt 4
                        if (!hasKaroBube && game.Type == GameType.Color)
                        {
                            fac += 1; // spielt 5
                            if (!(cards.Any((c) => c.Value == CardValue.Ass && c.Color == game.Color)))
                            {
                                fac += 1; // spielt 6
                                if (!(cards.Any((c) => c.Value == CardValue.Ziffer10 && c.Color == game.Color)))
                                {
                                    fac += 1; // spielt 7
                                    // @TODO: is going much better...
                                }
                            }
                        }
                    }
                }
            }
            return fac;
        }

        public static int GetPoints(List<Card> pointedCards)
        {
            var points = 0;
            foreach (var card in pointedCards)
            {
                switch (card.Value)
                {
                    case CardValue.Bube:
                        points += 2;
                        break;
                    case CardValue.Dame:
                        points += 3;
                        break;
                    case CardValue.Koenig:
                        points += 4;
                        break;
                    case CardValue.Ziffer10:
                        points += 10;
                        break;
                    case CardValue.Ass:
                        points += 11;
                        break;
                    default:
                        break;
                }
            }
            return points;
        }

        private static int GetColorFactor(CardColor cardColor)
        {
            switch (cardColor)
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
                    throw new ArgumentException("Invalid card color");
            }
        }

        public static int GetScore(int mult, List<Card> pointedCards, Game game, int fac = 1)
        {
            if (game.Type == GameType.Null)
            {
                if (game.Option == GameOption.Hand)
                {
                    return pointedCards.Count > 0 ? -118 * fac : 59 * fac;
                }
                return pointedCards.Count > 0 ? -46 * fac : 23 * fac;
            }
            if (game.Type == GameType.Grand || game.Type == GameType.Color)
            {
                int gameValue = 24;
                if (game.Type == GameType.Color)
                {
                    gameValue = GetColorFactor(game.Color.Value);
                }
                if (game.Option == GameOption.Hand)
                {
                    mult += 1;
                }
                var points = GetPoints(pointedCards);
                if (points<=60)
                {
                    if (points < 30)
                    {
                        mult += 1;
                    }
                    if (points == 0)
                    {
                        mult += 1;
                    }
                    return -gameValue * mult * fac * 2;
                }
                if (points > 90)
                {
                    mult += 1;
                }
                if (points == 120)
                {
                    mult += 1;
                }
                return gameValue * mult * fac;
            }
            return 0;
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
