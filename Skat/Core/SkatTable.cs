using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace MynaSkat.Core
{
    public enum ActionType { Bid, PassBid, HoldBid, PassHold, TakeSkat, StartGame, PlayHand, DoNotPlayHand };

    public class PlayerStatus
    {
        public string Header { get; set; } = "";

        public List<string> ActionLabels { get; set; } = new List<string>();

        public List<ActionType> ActionTypes { get; set; } = new List<ActionType>();
    };

    public class SkatTable
    {
        public int GameCounter { get; set; } = 1;

        public List<Player> Players { get; set; } = new List<Player>();

        public List<Card> Skat { get; set; } = new List<Card>();

        public List<Card> Stitch { get; set; } = new List<Card>();

        public List<Card> LastStitch { get; set; } = new List<Card>();

        public Player GamePlayer { get; set; } = null;

        public bool GameStarted { get; set; } = false;

        public MatadorsJackStraight MatadorsJackStraight { get; set; }

        public GameValue GameValue { get; set; }

        public Player CurrentPlayer { get; set; } = null;

        public bool SkatTaken { get; set; } = false;

        public bool GameEnded
        {
            get
            {
                return GameStarted && GamePlayer != null && !GamePlayer.Cards.Any() && !Stitch.Any();
            }
        }

        public int CurrentBidValue
        {
            get
            {
                if (BidValueIndex >= 0)
                {
                    return BidValues[BidValueIndex];
                }
                return 0;
            }
        }

        public int NextBidValue
        {
            get
            {
                if (BidValueIndex < BidValues.Count - 1)
                {
                    return BidValues[BidValueIndex + 1];
                }
                return 0;
            }
        }
       
        public bool BidSaid { get; set; } = false;

        public bool BidExceeded { get; set; } = false;

        private int BidValueIndex { get; set; } = -1;

        private List<int> BidValues = new List<int>();

        public SkatTable(string player1, string player2, string player3)
        {
            Players.Add(new Player(player1, PlayerPosition.Rearhand));
            Players.Add(new Player(player2, PlayerPosition.Forehand));
            Players.Add(new Player(player3, PlayerPosition.Middlehand));
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
            BidValues = s.ToList<int>();
            BidValues.Sort();
        }

        public void StartNewRound()
        {
            GameCounter += 1;
            foreach (var p in Players)
            {
                p.Stitches.Clear();
                p.Cards.Clear();
                p.Game = new Game(GameType.Grand);
                switch (p.Position)
                {

                    case PlayerPosition.Middlehand:
                        p.Position = PlayerPosition.Forehand;
                        p.BidStatus = BidStatus.Accept;
                        break;
                    case PlayerPosition.Rearhand:
                        p.Position = PlayerPosition.Middlehand;
                        p.BidStatus = BidStatus.Bid;
                        break;
                    case PlayerPosition.Forehand:
                        p.Position = PlayerPosition.Rearhand;
                        p.BidStatus = BidStatus.Wait;
                        break;
                    default:
                        break;
                }
            }
            MatadorsJackStraight = null;
            GameStarted = false;
            GamePlayer = null;
            GameValue = null;
            CurrentPlayer = null;
            Stitch.Clear();
            Skat.Clear();
            LastStitch.Clear();
            using (var rng = new RNGCryptoServiceProvider())
            {
                var deck = Card.GenerateDeck();
                foreach (var player in Players)
                {
                    player.Cards.AddRange(Card.Draw(rng, deck, 10));
                }
                Skat.AddRange(Card.Draw(rng, deck, 2));
            }
            BidSaid = false;
            BidValueIndex = -1;
            BidExceeded = false;
        }

        public void MoveNextBidValue()
        {
            if (BidValueIndex < BidValues.Count - 1)
            {
                BidValueIndex++;
            }
        }

        private int GetPlayerIdx(Player player)
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

        public Player GetStitchPlayer()
        {
            Player stichPlayer = null;
            if (Stitch.Count == 3)
            {
                stichPlayer = CurrentPlayer;
                Player player = stichPlayer;
                Card greatestCard = Stitch[0];
                Card firstCard = greatestCard;
                for (int idx=1; idx<3; idx++)
                {
                    player = GetNextPlayer(player);
                    if (IsTrump(firstCard) && IsTrump(Stitch[idx]) && IsCardGreater(GamePlayer.Game, Stitch[idx], greatestCard))
                    {
                        stichPlayer = player;
                        greatestCard = Stitch[idx];
                    }
                    else if (!IsTrump(firstCard) && IsTrump(Stitch[idx]) && IsCardGreater(GamePlayer.Game, Stitch[idx], greatestCard))
                    {
                        stichPlayer = player;
                        greatestCard = Stitch[idx];
                    }
                    else if (!IsTrump(firstCard) && firstCard.Color == Stitch[idx].Color && IsCardGreater(GamePlayer.Game, Stitch[idx], greatestCard))
                    {
                        stichPlayer = player;
                        greatestCard = Stitch[idx];
                    }
                }
            }
            return stichPlayer;
        }

        public bool IsValidForStitch(Card card)
        {
            if (Stitch.Count == 0) return true;
            var first = Stitch[0];
            if (IsTrump(first))
            {
                if (IsTrump(card))
                {
                    return true;
                }
                var hasTrump = CurrentPlayer.Cards.Any( c => IsTrump(c));
                return !hasTrump;
            }
            bool hasColor = CurrentPlayer.Cards.Any((c) => !IsTrump(c) && c.Color == first.Color);
            return !IsTrump(card) && first.Color == card.Color || !hasColor;
        }

        public bool IsTrump(Card card)
        {
            Game game = GamePlayer.Game;
            if ((game.Type == GameType.Grand || game.Type == GameType.Color) && card.Value == CardValue.Jack)
            {
                return true;
            }
            if (game.Type == GameType.Color && card.Color == game.Color)
            {
                return true;
            }
            return false;
        }

        private bool IsCardGreater(Game game, Card card1, Card card2)
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
                    p.Stitches.AddRange(GamePlayer.Cards);
                    p.Stitches.AddRange(GamePlayer.Stitches);
                    p.Stitches.AddRange(p.Cards);
                    p.Stitches.AddRange(Skat);
                    p.Stitches.AddRange(Stitch);
                    p.Cards.Clear();
                    GamePlayer.Cards.Clear();
                    GamePlayer.Stitches.Clear();
                    Skat.Clear();
                    Stitch.Clear();
                }
                else if (p != GamePlayer && opponentPlayer != null)
                {
                    opponentPlayer.Stitches.AddRange(p.Cards);
                    p.Cards.Clear();
                }
            }
            var game = GamePlayer.Game;
            GameValue = game.GetGameValue(MatadorsJackStraight, GamePlayer.Stitches, Skat, CurrentBidValue);
            GamePlayer.Score += GameValue.Score;
        }

        public bool CanViewLastStitch(Player player)
        {
            return GameStarted &&
                LastStitch.Count > 0 &&
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

        public bool CanStartNewGame()
        {
            return GameStarted && GamePlayer != null && GamePlayer.Cards.Count == 0 && Stitch.Count == 0;
        }

        public void StartGame(Player player)
        {
            List<Card> skat = null;
            if (!player.Game.Option.HasFlag(GameOption.Hand))
            {
                skat = Skat;
            }
            var jackStraight = player.Game.GetMatadorsJackStraight(player.Cards, skat);
            if (player.Game.GetBidValue(jackStraight) < CurrentBidValue)
            {
                BidExceeded = true;
            }
            GameStarted = true;
            foreach (var p in Players)
            {
                p.Game = player.Game; // same card sort order for everybody
                if (p.Position == PlayerPosition.Forehand)
                {
                    CurrentPlayer = p;
                }
            }
            // spitzen mit skat
            MatadorsJackStraight = player.Game.GetMatadorsJackStraight(player.Cards, skat);
        }

        public Player GetActivePlayer()
        {
            Player activePlayer = null;
            foreach (var p in Players)
            {
                if (GamePlayer == null)
                {
                    if (p.BidStatus == BidStatus.Bid && !BidSaid ||
                        p.BidStatus == BidStatus.Accept && BidSaid)
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

        public Player GetBidPlayer(BidStatus bidStatus)
        {
            foreach (var p in Players)
            {
                if (p.BidStatus == bidStatus)
                {
                    return p;
                }
            }
            return null;
        }

        public PlayerStatus GetPlayerStatus(Player player)
        {
            var ret = new PlayerStatus();
            // Bidding
            if (GamePlayer == null)
            {
                if (player.BidStatus == BidStatus.Wait)
                {
                    ret.Header += $"Du wartest bis du zum Reizen an der Reihe bist. ";
                    ret.Header += $"{GetBidPlayer(BidStatus.Bid).Name} sagt {GetBidPlayer(BidStatus.Accept).Name}. ";
                    if (CurrentBidValue > 0)
                    {
                        ret.Header += $"Es sind aktuell {CurrentBidValue} angesagt. ";
                    }
                }
                else if (player.BidStatus == BidStatus.Accept && !BidSaid)
                {
                    ret.Header += $"Du wartest auf eine Reizansage von {GetBidPlayer(BidStatus.Bid).Name}. ";
                    if (CurrentBidValue > 0)
                    {
                        ret.Header += $"Es sind aktuell {CurrentBidValue} angesagt. ";
                    }
                }
                else if (player.BidStatus == BidStatus.Accept && BidSaid)
                {
                    ret.Header += $"Du musst die Reizanfrage beantworten. ";
                    ret.Header += $"{GetBidPlayer(BidStatus.Bid).Name} hat {CurrentBidValue} gesagt. ";
                    ret.ActionLabels.Add($"{CurrentBidValue} halten");
                    ret.ActionLabels.Add("Weg");
                    ret.ActionTypes.Add(ActionType.HoldBid);
                    ret.ActionTypes.Add(ActionType.PassHold);
                }
                else if (player.BidStatus == BidStatus.Bid && !BidSaid)
                {
                    var acceptPlayer = GetBidPlayer(BidStatus.Accept);
                    if (acceptPlayer == null)
                    {
                        ret.Header += "Alle Spieler haben gepasst. Du kannst jetzt eine Reizansage abgeben oder auch passen. ";
                    }
                    else
                    {
                        ret.Header += $"Du musst eine Reizansage abgeben für {acceptPlayer.Name}. ";
                        if (CurrentBidValue > 0)
                        {
                            ret.Header += $"Es sind aktuell {CurrentBidValue} angesagt. ";
                        }
                    }
                    ret.ActionLabels.Add($"{NextBidValue} sagen");
                    ret.ActionLabels.Add("Weg");
                    ret.ActionTypes.Add(ActionType.Bid);
                    ret.ActionTypes.Add(ActionType.PassBid);
                }
                else if (player.BidStatus == BidStatus.Bid && BidSaid)
                {
                    ret.Header += $"Du wartest auf eine Antwort von {GetBidPlayer(BidStatus.Accept).Name}. Du hast {CurrentBidValue} angesagt. ";
                }
                else if (player.BidStatus == BidStatus.Pass)
                {
                    ret.Header += "Du hast beim Reizen gepasst. ";
                    var acceptPlayer = GetBidPlayer(BidStatus.Accept);
                    if (acceptPlayer != null)
                    {
                        ret.Header += $"{GetBidPlayer(BidStatus.Bid).Name} sagt {acceptPlayer.Name}. ";
                    }
                    else
                    {
                        ret.Header += $"{GetBidPlayer(BidStatus.Bid).Name} könnte spielen. ";
                    }
                    if (CurrentBidValue > 0)
                    {
                        ret.Header += $"Es sind aktuell {CurrentBidValue} angesagt. ";
                    }
                }
            }
            // Game selection
            else if (!GameStarted)
            {
                if (player == GamePlayer)
                {
                    if (Skat.Count < 2)
                    {
                        ret.Header += "Du musst 2 Karten drücken. ";
                        ret.Header += $"Du hast {player.Game.GetGameAndOptionText()} als Spiel ausgewählt. ";
                        ret.Header += $"Du hast {CurrentBidValue} gesagt. ";
                    }
                    else if (!SkatTaken && !player.Game.Option.HasFlag(GameOption.Hand))
                    {
                        ret.Header += "Du kannst den Skat nehmen oder Hand ansagen. ";
                        ret.Header += $"Du hast {player.Game.GetGameAndOptionText()} als Spiel ausgewählt. ";
                        ret.Header += $"Du hast {CurrentBidValue} gesagt. ";
                        ret.ActionLabels.Add("Skat nehmen");
                        ret.ActionLabels.Add("Hand spielen");
                        ret.ActionTypes.Add(ActionType.TakeSkat);
                        ret.ActionTypes.Add(ActionType.PlayHand);
                    }
                    else
                    {
                        ret.Header += "Du kannst jetzt ";
                        ret.ActionLabels.Add("Los geht's!");
                        ret.ActionTypes.Add(ActionType.StartGame);
                        if (player.Game.Option.HasFlag(GameOption.Hand))
                        {
                            ret.ActionLabels.Add("Kein Handspiel!");
                            ret.ActionTypes.Add(ActionType.DoNotPlayHand);
                            ret.Header += "das Handspiel zurücknehmen ";
                        }
                        else
                        {
                            ret.Header += "den Skat ändern ";
                        }
                        ret.Header += "oder das Spiel starten. ";
                        ret.Header += $"Du hast {player.Game.GetGameAndOptionText()} als Spiel ausgewählt. ";
                        ret.Header += $"Du hast {CurrentBidValue} gesagt. ";
                    }
                }
                else
                {
                    ret.Header += $"Du wartest auf die Spielansage von {GamePlayer.Name}. Es wurden {CurrentBidValue} angesagt. ";
                }
            }
            // Game started
            else
            {
                // Game ended
                if (player.Cards.Count == 0 && Stitch.Count == 0)
                {
                    ret.Header += "Das Spiel ist beendet. ";
                    // exclude Skat for Null games
                    ret.Header += $"Du hast {GetScore(player)} Augen. ";
                    var next1 = GetNextPlayer(player);
                    var next2 = GetNextPlayer(next1);
                    ret.Header += $"{next1.Name} hat {GetScore(next1)} Augen. ";
                    ret.Header += $"{next2.Name} hat {GetScore(next2)} Augen. ";
                    if (player == GamePlayer)
                    {
                        ret.Header += "Du hast gespielt und ";
                    }
                    else
                    {
                        ret.Header += $"{GamePlayer.Name} hat gespielt und ";
                    }
                    if (GameValue.IsWinner)
                    {
                        ret.Header += "gewonnen. ";
                    }
                    else
                    {
                        ret.Header += "verloren. ";
                    }
                    ret.Header += $"{GameValue.Description} ";
                }
                // Game in progress
                else
                {
                    if (player == CurrentPlayer)
                    {
                        if (Stitch.Count == 3)
                        {
                            ret.Header += "Du musst den Stich einsammeln. ";
                        }
                        else
                        {
                            ret.Header += "Du musst eine Karte ausspielen. ";
                        }
                    }
                    else
                    {
                        ret.Header += $"Du wartest bis {CurrentPlayer.Name} eine Karte gespielt hat. ";
                    }
                    if (player == GamePlayer)
                    {
                        ret.Header += $"Du spielst {GamePlayer.Game.GetGameAndOptionText()} mit {CurrentBidValue}. ";
                        if (player == CurrentPlayer && BidExceeded)
                        {
                            ret.Header += "Du hast dich überreizt. ";
                        }
                    }
                    else
                    {
                        ret.Header += $"{GamePlayer.Name} spielt {GamePlayer.Game.GetGameAndOptionText()} mit {CurrentBidValue}. ";
                    }
                }
            }
            return ret;
        }

        public int GetScore(Player player)
        {
            List<Card> skat = null;
            if (player == GamePlayer && player.Game.Type != GameType.Null)
            {
                skat = Skat;
            }
            return Card.GetScore(player.Stitches, skat);
        }

        public void PerformPlayerAction(Player player, ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.TakeSkat:
                    SkatTaken = true;
                    break;
                case ActionType.PlayHand:
                    SetGameOption(player, player.Game.Option | GameOption.Hand);
                    break;
                case ActionType.StartGame:
                    StartGame(player);
                    break;
                case ActionType.DoNotPlayHand:
                    var gameOption = player.Game.Option & ~GameOption.Hand;
                    if (player.Game.Type != GameType.Null)
                    {
                        gameOption &= ~GameOption.Ouvert;
                    }
                    SetGameOption(player, gameOption);
                    break;
                case ActionType.Bid:
                    BidSaid = true;
                    MoveNextBidValue();
                    break;
                case ActionType.PassBid:
                    player.BidStatus = BidStatus.Pass;
                    foreach (var p in Players)
                    {
                        if (p.Position == PlayerPosition.Rearhand && p.BidStatus != BidStatus.Pass)
                        {
                            p.BidStatus = BidStatus.Bid;
                            break;
                        }
                    }
                    BidSaid = false;
                    break;
                case ActionType.HoldBid:
                    BidSaid = false;
                    break;
                case ActionType.PassHold:
                    BidSaid = false;
                    player.BidStatus = BidStatus.Pass;
                    foreach (var p in Players)
                    {
                        if (p.Position == PlayerPosition.Rearhand && p.BidStatus != BidStatus.Pass) // weitersagen
                        {
                            p.BidStatus = BidStatus.Bid;
                        }
                        else if (p.Position == PlayerPosition.Middlehand && p.BidStatus != BidStatus.Pass) // hoeren
                        {
                            p.BidStatus = BidStatus.Accept;
                        }
                    }
                    break;
                default:
                    break;
            }
            if (actionType == ActionType.PassBid ||
                actionType == ActionType.PassHold ||
                actionType == ActionType.Bid)
            {
                // find if all player have given up
                Player gamePlayer = null;
                var cntPassen = 0;
                foreach (var p in Players)
                {
                    if (p.BidStatus != BidStatus.Pass)
                    {
                        gamePlayer = p;
                        continue;
                    }
                    cntPassen++;
                }
                // all gave up
                if (cntPassen == 3)
                {
                    StartNewRound();
                }
                // two player gave up, remaing playing is the game player
                else if (gamePlayer != null && cntPassen == 2)
                {
                    if (gamePlayer.Position == PlayerPosition.Forehand && CurrentBidValue == 0)
                    {
                        gamePlayer.BidStatus = BidStatus.Bid;
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

        public bool CanCollectStitch(Player player)
        {
            return GameStarted && CurrentPlayer == player && Stitch.Count == 3;
        }

        public void CollectStitch(Player player)
        {
            LastStitch.Clear();
            LastStitch.AddRange(Stitch);
            Stitch.Clear();
            if (CurrentPlayer == player &&
                GamePlayer == player &&
                player.Game.Type == GameType.Null && player.Stitches.Any())
            {
                foreach (var p in Players)
                {
                    p.Cards.Clear();
                }
            }
            if (player.Cards.Count == 0)
            {
                var game = GamePlayer.Game;
                GameValue = game.GetGameValue(MatadorsJackStraight, GamePlayer.Stitches, Skat, CurrentBidValue);
                GamePlayer.Score += GameValue.Score;
            }
        }

        public void PlayCard(Player player, Card card)
        {
            // druecken
            if (GamePlayer == player && SkatTaken && !GameStarted)
            {
                if (Skat.Count < 2)
                {
                    player.Cards.Remove(card);
                    Skat.Add(card);
                }
            }
            else if (GameStarted && CurrentPlayer == player)
            {
                if (Stitch.Count == 3)
                {
                    CollectStitch(player);
                    if (player.Cards.Count == 0)
                    {
                        return; // game ended
                    }
                }
                if (IsValidForStitch(card))
                {
                    player.Cards.Remove(card);
                    CurrentPlayer = GetNextPlayer(player);
                    Stitch.Add(card);
                    if (Stitch.Count == 3)
                    {
                        var stichPlayer = GetStitchPlayer();
                        stichPlayer.Stitches.AddRange(Stitch);
                        CurrentPlayer = stichPlayer;
                    }
                }
            }
        }

        public bool CanPickupSkat(Player player)
        {
            return GamePlayer == player && SkatTaken && !GameStarted;
        }

        public void PickupSkat(Player player, Card card)
        {
            Skat.Remove(card);
            player.Cards.Add(card);
        }
    }
}
