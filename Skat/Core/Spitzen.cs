using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MynaSkat.Core
{
    public class Spitzen
    {
        public int Anzahl { get; set; } = 0;

        public int Spielt { get { return Anzahl + 1; } }

        public bool Mit { get; set; } = false;
    }
}
