using System.Collections.Generic;

namespace MynaSkat.Core
{
    public enum PlayerPosition { Rearhand, Forehand, Middlehand };

    public enum BidStatus { Bid, Accept, Pass, Wait };

    public class Player
    {
        public Player(string name, PlayerPosition position)
        {
            Name = name;
            Position = position;
            if (position == PlayerPosition.Middlehand)
            {
                BidStatus = BidStatus.Bid;
            }
            else if (position == PlayerPosition.Forehand)
            {
                BidStatus = BidStatus.Accept;
            }
            else
            {
                BidStatus = BidStatus.Wait;
            }
        }

        public string Name { get; set; }

        public PlayerPosition Position { get; set; }

        public Game Game { get; set; } = new Game(GameType.Grand);

        public List<Card> Cards { get; set; } = new List<Card>();

        public List<Card> Stitches { get; set; } = new List<Card>();

        public BidStatus BidStatus { get; set; } = BidStatus.Wait;

        public int Score { get; set; }

        public void SortCards()
        {
            Cards.Sort((b, a) => a.GetOrderNumber(Game).CompareTo(b.GetOrderNumber(Game)));
        }

        public string GetPositionText()
        {
            if (Position == PlayerPosition.Rearhand) return "Hinterhand";
            if (Position == PlayerPosition.Forehand) return "Vorhand";
            return "Mittelhand";
        }

        public override bool Equals(object obj)
        {
            var p = obj as Player;
            if (p != null)
            {
                return p.Name == Name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Name);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
