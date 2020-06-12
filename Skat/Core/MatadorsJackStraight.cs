namespace MynaSkat.Core
{
    public class MatadorsJackStraight
    {
        public int Count { get; set; } = 0;

        public int Play { get { return Count + 1; } }

        public bool With { get; set; } = false;
    }
}
