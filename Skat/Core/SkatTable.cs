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
        public int Spiele { get; set; } = 0;

        public List<Player> Players { get; set; } = new List<Player>();

        public List<Card> Skat { get; set; } = new List<Card>();

        public List<Card> Stich { get; set; } = new List<Card>();

        public List<Card> LetzterStich { get; set; } = new List<Card>();

        public Player GamePlayer { get; set; } = null;

        public bool GameStarted { get; set; } = false;

        public Spitzen Spitzen { get; set; }

        public Spielwert Spielwert { get; set; }

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

        public bool UeberReizt { get; set; } = false;

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

        public void StartNewRound()
        {
            foreach (var p in Players)
            {
                p.Stiche.Clear();
                p.Cards.Clear();
                p.Game = new Game(GameType.Grand);
                switch (p.Position)
                {

                    case PlayerPosition.Sagen:
                        p.Position = PlayerPosition.Hoeren;
                        p.ReizStatus = ReizStatus.Hoeren;
                        break;
                    case PlayerPosition.Geben:
                        p.Position = PlayerPosition.Sagen;
                        p.ReizStatus = ReizStatus.Sagen;
                        break;
                    case PlayerPosition.Hoeren:
                        p.Position = PlayerPosition.Geben;
                        p.ReizStatus = ReizStatus.Warten;
                        break;
                    default:
                        break;
                }
            }
            Spitzen = null;
            GameStarted = false;
            GamePlayer = null;
            Spielwert = null;
            CurrentPlayer = null;
            Stich.Clear();
            Skat.Clear();
            LetzterStich.Clear();
            using (var rng = new RNGCryptoServiceProvider())
            {
                var deck = Card.GenerateDeck();
                foreach (var player in Players)
                {
                    player.Cards.AddRange(Card.Draw(rng, deck, 10));
                }
                Skat.AddRange(Card.Draw(rng, deck, 2));
            }
            ReizSaid = false;
            ReizValueIndex = -1;
            UeberReizt = false;
        }

        public void MoveNextReizValue()
        {
            if (ReizValueIndex < ReizValues.Count - 1)
            {
                ReizValueIndex++;
            }
        }

        public int GetPlayerIdx(Player player)
        {
            int idx = 0;
            foreach(var p in Players)
            {
                if (p == player) return idx;
                idx++;
            }
            return -1;
        }

        public Player GetNextPlayer(Player player)
        {
            if (player != null)
            {
                var idx = GetPlayerIdx(player);
                var nextidx = (idx + 1) % Players.Count;
                return Players[nextidx];
            }
            return null;
        }

        public Player GetStichPlayer()
        {
            Player stichPlayer = null;
            if (Stich.Count == 3)
            {
                stichPlayer = CurrentPlayer;
                Player player = stichPlayer;
                Card greatestCard = Stich[0];
                Card firstCard = greatestCard;
                for (int idx=1; idx<3; idx++)
                {
                    player = GetNextPlayer(player);
                    if (IsTrumpf(firstCard) && IsTrumpf(Stich[idx]) && IsCardGreater(GamePlayer.Game, Stich[idx], greatestCard))
                    {
                        stichPlayer = player;
                        greatestCard = Stich[idx];
                    }
                    else if (!IsTrumpf(firstCard) && IsTrumpf(Stich[idx]) && IsCardGreater(GamePlayer.Game, Stich[idx], greatestCard))
                    {
                        stichPlayer = player;
                        greatestCard = Stich[idx];
                    }
                    else if (!IsTrumpf(firstCard) && firstCard.Color == Stich[idx].Color && IsCardGreater(GamePlayer.Game, Stich[idx], greatestCard))
                    {
                        stichPlayer = player;
                        greatestCard = Stich[idx];
                    }
                }
            }
            return stichPlayer;
        }

        public bool IsValidForStich(Card card)
        {
            if (Stich.Count == 0) return true;
            var first = Stich[0];
            if (IsTrumpf(first))
            {
                if (IsTrumpf(card))
                {
                    return true;
                }
                var hasTrumpf = CurrentPlayer.Cards.Any( c => IsTrumpf(c));
                return !hasTrumpf;
            }
            bool hasColor = CurrentPlayer.Cards.Any((c) => !IsTrumpf(c) && c.Color == first.Color);
            return first.Color == card.Color || !hasColor;
        }

        public bool IsTrumpf(Card card)
        {
            Game game = GamePlayer.Game;
            if ((game.Type == GameType.Grand || game.Type == GameType.Color) && card.Value == CardValue.Bube)
            {
                return true;
            }
            if (game.Type == GameType.Color && card.Color == game.Color)
            {
                return true;
            }
            return false;
        }

        public bool IsCardGreater(Game game, Card card1, Card card2)
        {
            return card1.GetOrderNumber(game) > card2.GetOrderNumber(game);
        }
    }
}
