namespace MynaSkat.Core
{
    public class GameValue
    {
        public int Score { get; set; }

        public string Description { get; set; } = "";

        public bool BidExceeded { get; set; }

        public bool IsWinner { get; set; } = true;
    }
}
