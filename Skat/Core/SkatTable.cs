using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MynaSkat.Core
{
    public class SkatTable
    {
        public List<Player> Players { get; set; } = new List<Player>();

        public List<Card> Skat { get; set; } = new List<Card>();

        public Player GamePlayer { get; set; } = null;

        public bool GameStarted { get; set; } = false;

        public Player CurrentPlayer { get; set; } = null;

        public bool SkatTaken { get; set; } = false;

        public int CurrentReizValue
        {
            get
            {
                if (ReizValueIndex >= 0)
                {
                    return ReizValues[ReizValueIndex];
                }
                return 0;
            }
        }

        public int NextReizValue
        {
            get
            {
                if (ReizValueIndex < ReizValues.Count - 1)
                {
                    return ReizValues[ReizValueIndex + 1];
                }
                return 0;
            }
        }

       
        public bool ReizSaid { get; set; } = false;

        private int ReizValueIndex { get; set; } = -1;

        private List<int> ReizValues = new List<int>();

        public SkatTable(string player1, string player2, string player3)
        {
            Players.Add(new Player(player1, PlayerPosition.Geben));
            Players.Add(new Player(player2, PlayerPosition.Hoeren));
            Players.Add(new Player(player3, PlayerPosition.Sagen));
            using (var rng = new RNGCryptoServiceProvider())
            {
                var deck = Card.GenerateDeck();
                foreach (var player in Players)
                {
                    player.Cards.AddRange(Card.Draw(rng, deck, 10));
                }
                Skat.AddRange(Card.Draw(rng, deck, 2));
            }
            var s = new HashSet<int>();
            // farbe
            for (int m = 2; m<18; m++) // mit 10 spielt 11 hand 12 schneider 13 angesagt 14 schwarz 15 angesagt 16 ouvert 17
            {
                s.Add(m * 9);
                s.Add(m * 10);
                s.Add(m * 11);
                s.Add(m * 12);
            }
            // grand
            for (int m = 2; m<12; m++) // mit 4 spielt 5 hand 6 schneider 7 angesagt 8 schwarz 9 angesagt 10 overt 11
            {
                s.Add(m * 24);
            }
            // null
            s.Add(23);
            // null hand
            s.Add(35);
            // null ouvert
            s.Add(46);
            // null ouvert hand
            s.Add(59);
            ReizValues = s.ToList<int>();
            ReizValues.Sort();
        }

        public void MoveNextReizValue()
        {
            if (ReizValueIndex < ReizValues.Count - 1)
            {
                ReizValueIndex++;
            }
        }
    }
}
