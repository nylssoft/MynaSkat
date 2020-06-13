using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace MynaSkat.Core
{
    public class PlayerStatus
    {
        public string Status { get; set; } = "";

        public string Game { get; set; } = "";

        public List<string> ActionLabels { get; set; } = new List<string>();

        public List<PlayerAction> Actions { get; set; } = new List<PlayerAction>();
    };

    public enum PlayerAction { Bid, PassBid, HoldBid, PassHold, TakeSkat, StartGame, PlayHand, DoNotPlayHand };

    public class SkatTable
    {
        public int TotalGames { get; set; } = 0;

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
            Players.Add(new Player(player1, BidOrder.Deal));
            Players.Add(new Player(player2, BidOrder.Response));
            Players.Add(new Player(player3, BidOrder.Bid));
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
            foreach (var p in Players)
            {
                p.Stitches.Clear();
                p.Cards.Clear();
                p.Game = new Game(GameType.Grand);
                switch (p.BidOrder)
                {

                    case BidOrder.Bid:
                        p.BidOrder = BidOrder.Response;
                        p.BidStatus = BidStatus.Accept;
                        break;
                    case BidOrder.Deal:
                        p.BidOrder = BidOrder.Bid;
                        p.BidStatus = BidStatus.Bid;
                        break;
                    case BidOrder.Response:
                        p.BidOrder = BidOrder.Deal;
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
            return first.Color == card.Color || !hasColor;
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
            TotalGames += 1;
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
                if (p.BidOrder == BidOrder.Response)
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

        public PlayerStatus GetPlayerStatus(Player viewPlayer, Player player)
        {
            var ret = new PlayerStatus();
            // Reizen
            if (GamePlayer == null)
            {
                if (player.BidStatus == BidStatus.Wait)
                {
                    ret.Status = "Wartet.";
                }
                else if (player.BidStatus == BidStatus.Accept && !BidSaid)
                {
                    ret.Status = "Hört auf Reizansage.";
                    if (CurrentBidValue > 0)
                    {
                        ret.Status += $" {CurrentBidValue} angesagt.";
                    }
                }
                else if (player.BidStatus == BidStatus.Accept && BidSaid)
                {
                    ret.Status = "Antworten!";
                    if (viewPlayer == player)
                    {
                        ret.ActionLabels.Add($"{CurrentBidValue} halten");
                        ret.ActionLabels.Add("Weg");
                        ret.Actions.Add(PlayerAction.HoldBid);
                        ret.Actions.Add(PlayerAction.PassHold);
                    }
                }
                else if (player.BidStatus == BidStatus.Bid && !BidSaid)
                {
                    ret.Status = "Reizen!";
                    if (viewPlayer == player)
                    {
                        ret.ActionLabels.Add($"{NextBidValue} sagen");
                        ret.ActionLabels.Add("Weg");
                        ret.Actions.Add(PlayerAction.Bid);
                        ret.Actions.Add(PlayerAction.PassBid);
                    }
                }
                else if (player.BidStatus == BidStatus.Bid && BidSaid)
                {
                    ret.Status = $"Wartet auf Antwort. {CurrentBidValue} gesagt.";
                }
                else if (player.BidStatus == BidStatus.Pass)
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
                            ret.ActionLabels.Add("Skat nehmen");
                            ret.ActionLabels.Add("Hand spielen");
                            ret.Actions.Add(PlayerAction.TakeSkat);
                            ret.Actions.Add(PlayerAction.PlayHand);
                            ret.Game += $"Du wirst {viewPlayer.Game.GetGameAndOptionText()} spielen. ";
                        }
                    }
                    else
                    {
                        ret.Status = "Spiel ansagen oder drücken!";
                        if (viewPlayer == player)
                        {
                            ret.ActionLabels.Add("Los geht's!");
                            ret.Actions.Add(PlayerAction.StartGame);
                            if (player.Game.Option.HasFlag(GameOption.Hand))
                            {
                                ret.ActionLabels.Add("Kein Handspiel!");
                                ret.Actions.Add(PlayerAction.DoNotPlayHand);
                            }
                            ret.Game += $"Du wirst {viewPlayer.Game.GetGameAndOptionText()} spielen. ";
                        }
                    }
                    ret.Status += $" Du hast {CurrentBidValue} angesagt.";
                }
            }
            // Game started
            else
            {
                // Game ended
                if (player.Cards.Count == 0 && Stitch.Count == 0)
                {
                    ret.Status = "Spiel beendet.";
                    List<Card> skat = null;
                    if (player == GamePlayer && player.Game.Type != GameType.Null)
                    {
                        skat = Skat;
                    }
                    ret.Game += $"{Card.GetScore(player.Stitches, skat)} Augen. ";
                    if (player == GamePlayer)
                    {
                        ret.Game += $"{GameValue.Description} ";
                    }
                }
                // Game in progress
                else
                {
                    if (player == CurrentPlayer)
                    {
                        if (Stitch.Count == 3)
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
                        if (player == CurrentPlayer && BidExceeded)
                        {
                            ret.Game += "Überreizt! ";
                        }
                        ret.Game += $"Spielt {viewPlayer.Game.GetGameAndOptionText()}. ";
                        ret.Game += $"Hat {CurrentBidValue} gesagt. ";
                    }
                }
            }
            if (!GameStarted)
            {
                if (player.BidOrder == BidOrder.Deal)
                {
                    ret.Status += " Hat gegeben.";
                }
                else if (player.BidOrder == BidOrder.Response)
                {
                    ret.Status += " Kommt aus.";
                }
            }
            if (TotalGames > 0)
            {
                ret.Game += $"{player.Score} Punkte.";
                if (TotalGames > 1)
                {
                    ret.Game += $" {TotalGames} Spiele.";
                }
                else
                {
                    ret.Game += $" {TotalGames} Spiel.";
                }
            }
            return ret;
        }

        public void PerformPlayerAction(Player player, PlayerAction playerAction)
        {
            switch (playerAction)
            {
                case PlayerAction.TakeSkat:
                    SkatTaken = true;
                    break;
                case PlayerAction.PlayHand:
                    SetGameOption(player, player.Game.Option | GameOption.Hand);
                    break;
                case PlayerAction.StartGame:
                    StartGame(player);
                    break;
                case PlayerAction.DoNotPlayHand:
                    var gameOption = player.Game.Option & ~GameOption.Hand;
                    if (player.Game.Type != GameType.Null)
                    {
                        gameOption &= ~GameOption.Ouvert;
                    }
                    SetGameOption(player, gameOption);
                    break;
                case PlayerAction.Bid:
                    BidSaid = true;
                    MoveNextBidValue();
                    break;
                case PlayerAction.PassBid:
                    player.BidStatus = BidStatus.Pass;
                    foreach (var p in Players)
                    {
                        if (p.BidOrder == BidOrder.Deal && p.BidStatus != BidStatus.Pass)
                        {
                            p.BidStatus = BidStatus.Bid;
                            break;
                        }
                    }
                    BidSaid = false;
                    break;
                case PlayerAction.HoldBid:
                    BidSaid = false;
                    break;
                case PlayerAction.PassHold:
                    BidSaid = false;
                    player.BidStatus = BidStatus.Pass;
                    foreach (var p in Players)
                    {
                        if (p.BidOrder == BidOrder.Deal && p.BidStatus != BidStatus.Pass) // weitersagen
                        {
                            p.BidStatus = BidStatus.Bid;
                        }
                        else if (p.BidOrder == BidOrder.Bid && p.BidStatus != BidStatus.Pass) // hoeren
                        {
                            p.BidStatus = BidStatus.Accept;
                        }
                    }
                    break;
                default:
                    break;
            }
            if (playerAction == PlayerAction.PassBid ||
                playerAction == PlayerAction.PassHold ||
                playerAction == PlayerAction.Bid)
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
                    TotalGames += 1;
                    StartNewRound();
                }
                // two player gave up, remaing playing is the game player
                else if (gamePlayer != null && cntPassen == 2)
                {
                    if (gamePlayer.BidOrder == BidOrder.Response && CurrentBidValue == 0)
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

        public void CollectStitch(Player player)
        {
            if (GameStarted && CurrentPlayer == player && Stitch.Count >= 3)
            {
                LastStitch.Clear();
                LastStitch.AddRange(Stitch);
                Stitch.Clear();
            }
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
                TotalGames += 1;
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
                    if (player.Cards.Count == 0) // @TODO: game ended
                    {
                        card = null;
                    }
                }
                if (card != null && IsValidForStitch(card))
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
