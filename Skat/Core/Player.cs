using System.Collections.Generic;

namespace MynaSkat.Core
{
    public enum BidOrder { Deal, Response, Bid, Continue };

    public enum BidStatus { Bid, Accept, Pass, Wait };

    public class Player
    {
        public Player(string name, BidOrder position)
        {
            Name = name;
            BidOrder = position;
            if (position == BidOrder.Bid)
            {
                BidStatus = BidStatus.Bid;
            }
            else if (position == BidOrder.Response)
            {
                BidStatus = BidStatus.Accept;
            }
            else
            {
                BidStatus = BidStatus.Wait;
            }
        }

        public string Name { get; set; }

        public BidOrder BidOrder { get; set; }

        public Game Game { get; set; } = new Game(GameType.Grand);

        public List<Card> Cards { get; set; } = new List<Card>();

        public List<Card> Stitches { get; set; } = new List<Card>();

        public BidStatus BidStatus { get; set; } = BidStatus.Wait;

        public int Score { get; set; }

        public void SortCards()
        {
            Cards.Sort((b, a) => a.GetOrderNumber(Game).CompareTo(b.GetOrderNumber(Game)));
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
            return $"Spieler '{Name}', {BidOrder}, {BidStatus}";
        }
    }
}
