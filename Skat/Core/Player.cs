﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MynaSkat.Core
{
    public enum PlayerPosition { Geben, Hoeren, Sagen, Weitersagen };

    public enum ReizStatus { Sagen, Hoeren, Passen, Warten };

    public class Player
    {
        public Player(string name, PlayerPosition position)
        {
            Name = name;
            Position = position;
            if (position == PlayerPosition.Sagen)
            {
                ReizStatus = ReizStatus.Sagen;
            }
            else if (position == PlayerPosition.Hoeren)
            {
                ReizStatus = ReizStatus.Hoeren;
            }
            else
            {
                ReizStatus = ReizStatus.Warten;
            }
        }

        public string Name { get; set; }

        public PlayerPosition Position { get; set; }

        public Game Game { get; set; } = new Game(GameType.Grand);

        public List<Card> Cards { get; set; } = new List<Card>();

        public List<Card> Points { get; set; } = new List<Card>();

        public ReizStatus ReizStatus { get; set; } = ReizStatus.Warten;

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
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return $"Spieler '{Name}', {Position}, {ReizStatus}";
        }
    }
}