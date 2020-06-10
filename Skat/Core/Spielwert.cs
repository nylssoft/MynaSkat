using System;
using System.Collections.Generic;
using System.Text;

namespace MynaSkat.Core
{
    public class Spielwert
    {
        public int Punkte { get; set; }

        public string Beschreibung { get; set; } = "";

        public bool Ueberreizt { get; set; }

        public bool Gewonnen { get; set; } = true;
    }
}
