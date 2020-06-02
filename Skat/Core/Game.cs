using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MynaSkat.Core
{
    public enum GameType { Grand, Color, Null };

    [Flags]
    public enum GameOption { None = 0, Ouvert = 1, Hand = 2, Schneider = 4, Schwarz = 8 };

    public class Game
    {
        public GameType Type { get; set; } = GameType.Grand;

        public GameOption Option { get; set; } = GameOption.None;

        public CardColor? Color { get; set; } = null;

        public Game(GameType gameType, CardColor? gameColor = null)
        {
            Type = gameType;
            Color = gameColor;
        }

        public override bool Equals(object obj)
        {
            var g = obj as Game;
            if (g != null)
            {
                return g.Type == Type && g.Color == Color && g.Option == Option;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var ret = Type.GetHashCode();
            if (Color != null)
            {
                ret += Color.GetHashCode() * 27;
            }
            ret += (int)Option * 113;
            return ret;
        }

        public override string ToString()
        {
            string ret;
            if (Type != GameType.Color)
            {
                ret = $"{Type}";
            }
            else
            {
                ret = $"{Color}";
            }
            if (Option != GameOption.None)
            {
                ret += $" {Option}";
            }
            return ret;
        }
    }
}
