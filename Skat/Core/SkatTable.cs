using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MynaSkat.Core
{
    public class PlayerStatus
    {
        public string Status { get; set; } = "";

        public string Game { get; set; } = "";

        public List<string> Actions { get; set; } = new List<string>();
    };

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
                    player.SortCards();
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

        public bool CanGiveUp(Player player)
        {
            return GameStarted &&
                player == GamePlayer &&
                player == CurrentPlayer &&
                player.Cards.Count >= 9;
        }

        public void GiveUp()
        {
            // add all cards on the game player's hand to the stitch of one opponent player
            Player opponentPlayer = null;
            foreach (var p in Players)
            {
                if (p != GamePlayer && opponentPlayer == null)
                {
                    opponentPlayer = p;
                    p.Stiche.AddRange(GamePlayer.Cards);
                    p.Stiche.AddRange(GamePlayer.Stiche);
                    p.Stiche.AddRange(p.Cards);
                    p.Stiche.AddRange(Skat);
                    p.Stiche.AddRange(Stich);
                    p.Cards.Clear();
                    GamePlayer.Cards.Clear();
                    GamePlayer.Stiche.Clear();
                    Skat.Clear();
                    Stich.Clear();
                }
                else if (p != GamePlayer && opponentPlayer != null)
                {
                    opponentPlayer.Stiche.AddRange(p.Cards);
                    p.Cards.Clear();
                }
            }
            var game = GamePlayer.Game;
            Spielwert = game.GetSpielWert(Spitzen, GamePlayer.Stiche, Skat, CurrentReizValue);
            GamePlayer.Score += Spielwert.Punkte;
            Spiele += 1;
        }

        public bool CanViewLastStitch(Player player)
        {
            return GameStarted &&
                LetzterStich.Count > 0 &&
                player == CurrentPlayer &&
                player.Cards.Count > 0;
        }

        public bool CanSetOuvert(Player player)
        {
            return !GameStarted && GamePlayer == player && (GamePlayer.Game.Type == GameType.Null || !SkatTaken);
        }

        public bool CanSetHand(Player player)
        {
            return !GameStarted && !SkatTaken && GamePlayer == player && (!GamePlayer.Game.Option.HasFlag(GameOption.Ouvert) || GamePlayer.Game.Type == GameType.Null);
        }

        public bool CanSetSchneider(Player player)
        {
            return CanSetHand(player) &&
                   GamePlayer.Game.Type != GameType.Null &&
                   GamePlayer.Game.Option.HasFlag(GameOption.Hand) &&
                   !GamePlayer.Game.Option.HasFlag(GameOption.Ouvert);
        }

        public bool CanSetSchwarz(Player player)
        {
            return CanSetSchneider(player) &&
                   GamePlayer.Game.Option.HasFlag(GameOption.Schneider);
        }

        public void SetGameOption(Player player, GameOption gameOption)
        {
            player.Game.Option = GameOption.None;
            if (player.Game.Type == GameType.Null)
            {
                if (gameOption.HasFlag(GameOption.Ouvert))
                {
                    player.Game.Option |= GameOption.Ouvert;
                }
                if (gameOption.HasFlag(GameOption.Hand))
                {
                    player.Game.Option |= GameOption.Hand;
                }
            }
            else
            {
                if (gameOption.HasFlag(GameOption.Ouvert))
                {
                    player.Game.Option |= GameOption.Ouvert | GameOption.Hand | GameOption.Schneider | GameOption.Schwarz;
                }
                else if (gameOption.HasFlag(GameOption.Hand))
                {
                    player.Game.Option |= GameOption.Hand;
                    if (gameOption.HasFlag(GameOption.Schneider))
                    {
                        player.Game.Option |= GameOption.Schneider;
                        if (gameOption.HasFlag(GameOption.Schwarz))
                        {
                            player.Game.Option |= GameOption.Schwarz;
                        }
                    }
                }
            }
        }

        public void StartGame(Player player)
        {
            List<Card> skat = null;
            if (!player.Game.Option.HasFlag(GameOption.Hand))
            {
                skat = Skat;
            }
            var spitzen = player.Game.GetSpitzen(player.Cards, skat);
            var reizvalue = player.Game.GetReizWert(spitzen);
            if (reizvalue < CurrentReizValue)
            {
                UeberReizt = true;
            }
            GameStarted = true;
            foreach (var p in Players)
            {
                p.Game = player.Game; // same card sort order for everybody
                if (p.Position == PlayerPosition.Hoeren)
                {
                    CurrentPlayer = p;
                }
            }
            // spitzen mit skat
            Spitzen = player.Game.GetSpitzen(player.Cards, skat);
        }

        public Player GetActivePlayer()
        {
            Player activePlayer = null;
            foreach (var p in Players)
            {
                if (GamePlayer == null)
                {
                    if (p.ReizStatus == ReizStatus.Sagen && !ReizSaid ||
                        p.ReizStatus == ReizStatus.Hoeren && ReizSaid)
                    {
                        activePlayer = p;
                        break;
                    }
                }
                else
                {
                    if (CurrentPlayer == null)
                    {
                        activePlayer = GamePlayer;
                        break;
                    }
                    activePlayer = CurrentPlayer;
                    break;
                }
            }
            return activePlayer;
        }

        public PlayerStatus GetPlayerStatus(Player viewPlayer, Player player)
        {
            var ret = new PlayerStatus();
            // Reizen
            if (GamePlayer == null)
            {
                if (player.ReizStatus == ReizStatus.Warten)
                {
                    ret.Status = "Wartet.";
                }
                else if (player.ReizStatus == ReizStatus.Hoeren && !ReizSaid)
                {
                    ret.Status = "Hört auf Reizansage.";
                    if (CurrentReizValue > 0)
                    {
                        ret.Status += $" {CurrentReizValue} angesagt.";
                    }
                }
                else if (player.ReizStatus == ReizStatus.Hoeren && ReizSaid)
                {
                    ret.Status = "Antworten!";
                    if (viewPlayer == player)
                    {
                        ret.Actions.Add($"{CurrentReizValue} halten");
                        ret.Actions.Add("Weg");
                    }
                }
                else if (player.ReizStatus == ReizStatus.Sagen && !ReizSaid)
                {
                    ret.Status = "Reizen!";
                    if (viewPlayer == player)
                    {
                        ret.Actions.Add($"{NextReizValue} sagen");
                        ret.Actions.Add("Weg");
                    }
                }
                else if (player.ReizStatus == ReizStatus.Sagen && ReizSaid)
                {
                    ret.Status = $"Wartet auf Antwort. {CurrentReizValue} gesagt.";
                }
                else if (player.ReizStatus == ReizStatus.Passen)
                {
                    ret.Status = "Weg.";
                }
            }
            // Game selection
            else if (!GameStarted)
            {
                ret.Status = $"Wartet auf Spielansage von {GamePlayer.Name}.";
                if (player == GamePlayer)
                {
                    if (Skat.Count < 2)
                    {
                        ret.Status = "Drücken!";
                    }
                    else if (!SkatTaken && !player.Game.Option.HasFlag(GameOption.Hand))
                    {
                        ret.Status = "Skat nehmen oder Hand ansagen!";
                        if (viewPlayer == player)
                        {
                            ret.Actions.Add("Skat nehmen");
                            ret.Actions.Add("Hand spielen");
                            ret.Game += $"Du wirst {viewPlayer.Game.GetGameText()} spielen. ";
                        }
                    }
                    else
                    {
                        ret.Status = "Spiel ansagen oder drücken!";
                        if (viewPlayer == player)
                        {
                            ret.Actions.Add("Los geht's!");
                            if (player.Game.Option.HasFlag(GameOption.Hand))
                            {
                                ret.Actions.Add("Kein Handspiel!");
                            }
                            ret.Game += $"Du wirst {viewPlayer.Game.GetGameText()} spielen. ";
                        }
                    }
                    ret.Status += $" Du hast {CurrentReizValue} angesagt.";
                }
            }
            // Game started
            else
            {
                // Game ended
                if (player.Cards.Count == 0 && Stich.Count == 0)
                {
                    ret.Status = "Spiel beendet.";
                    List<Card> skat = null;
                    if (player == GamePlayer && player.Game.Type != GameType.Null)
                    {
                        skat = Skat;
                    }
                    ret.Game += $"{Card.GetAugen(player.Stiche, skat)} Augen. ";
                    if (player == GamePlayer)
                    {
                        ret.Game += $"{Spielwert.Beschreibung} ";
                    }
                }
                // Game in progress
                else
                {
                    if (player == CurrentPlayer)
                    {
                        if (Stich.Count == 3)
                        {
                            ret.Status = "Stich einsammeln!";
                        }
                        else
                        {
                            ret.Status = "Ausspielen!";
                        }
                    }
                    else
                    {
                        ret.Status = $"Wartet auf {CurrentPlayer.Name}. ";
                    }
                    if (player == GamePlayer)
                    {
                        if (player == CurrentPlayer && UeberReizt)
                        {
                            ret.Game += "Überreizt! ";
                        }
                        ret.Game += $"Spielt {viewPlayer.Game.GetGameText()}. ";
                        ret.Game += $"Hat {CurrentReizValue} gesagt. ";
                    }
                }
            }
            // player position
            if (player.Position == PlayerPosition.Geben)
            {
                ret.Status += " Hat gegeben.";
            }
            if (Spiele > 0)
            {
                ret.Game += $"{player.Score} Punkte.";
                if (Spiele > 1)
                {
                    ret.Game += $" {Spiele} Spiele.";
                }
                else
                {
                    ret.Game += $" {Spiele} Spiel.";
                }
            }
            return ret;
        }

        public void PerformPlayerAction(Player player, bool isAction1, bool isAction2)
        {
            // Game actions
            if (GamePlayer != null)
            {
                // Take skat or enable Hand option
                if (!SkatTaken && !player.Game.Option.HasFlag(GameOption.Hand))
                {
                    // Take skat
                    if (isAction1)
                    {
                        SkatTaken = true;
                    }
                    // Enable Hand option
                    else if (isAction2)
                    {
                        SetGameOption(player, player.Game.Option | GameOption.Hand);
                    }
                }
                // Skat taken or Hand option
                else
                {
                    // Start game
                    if (isAction1)
                    {
                        StartGame(player);
                    }
                    // Remove Hand option
                    else if (isAction2)
                    {
                        var gameOption = player.Game.Option & ~GameOption.Hand;
                        if (player.Game.Type != GameType.Null)
                        {
                            gameOption &= ~GameOption.Ouvert;
                        }
                        SetGameOption(player, gameOption);
                    }
                }
            }
            // Reizen actions
            else
            {
                // Player is saying but has not yet said a value
                if (player.ReizStatus == ReizStatus.Sagen && !ReizSaid)
                {
                    // Said current value
                    if (isAction1)
                    {
                        ReizSaid = true;
                        MoveNextReizValue();
                    }
                    // Give up, move to next player to say a value
                    else if (isAction2)
                    {
                        player.ReizStatus = ReizStatus.Passen;
                        foreach (var p in Players)
                        {
                            if (p.Position == PlayerPosition.Geben && p.ReizStatus != ReizStatus.Passen)
                            {
                                p.ReizStatus = ReizStatus.Sagen;
                                break;
                            }
                        }
                        ReizSaid = false;
                    }
                }
                // Player is hearing a value
                else if (player.ReizStatus == ReizStatus.Hoeren)
                {
                    // Acknowledge value
                    if (isAction1)
                    {
                        ReizSaid = false;
                    }
                    // Give up, move to next player to say a value
                    else if (isAction2)
                    {
                        ReizSaid = false;
                        player.ReizStatus = ReizStatus.Passen;
                        foreach (var p in Players)
                        {
                            if (p.Position == PlayerPosition.Geben && p.ReizStatus != ReizStatus.Passen) // weitersagen
                            {
                                p.ReizStatus = ReizStatus.Sagen;
                            }
                            else if (p.Position == PlayerPosition.Sagen && p.ReizStatus != ReizStatus.Passen) // hoeren
                            {
                                p.ReizStatus = ReizStatus.Hoeren;
                            }
                        }
                    }
                }
                // find if all player have given up
                Player gamePlayer = null;
                var cntPassen = 0;
                foreach (var p in Players)
                {
                    if (p.ReizStatus != ReizStatus.Passen)
                    {
                        gamePlayer = p;
                        continue;
                    }
                    cntPassen++;
                }
                // all gave up
                if (cntPassen == 3)
                {
                    Spiele += 1;
                    StartNewRound();
                }
                // two player gave up, remaing playing is the game player
                else if (gamePlayer != null && cntPassen == 2)
                {
                    if (gamePlayer.Position == PlayerPosition.Hoeren && CurrentReizValue == 0)
                    {
                        gamePlayer.ReizStatus = ReizStatus.Sagen;
                    }
                    else
                    {
                        GamePlayer = gamePlayer;
                        GameStarted = false;
                        SkatTaken = false;
                    }
                }
            }
        }
    }
}
