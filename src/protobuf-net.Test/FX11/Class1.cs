using System;
#if !FX11
using System.Collections.Generic;
#endif
using System.Text;
using System.Diagnostics;

namespace SampleDto
{
    public class Customer
    {
        private int id;
        public int Id { get { return id; } set { id = value; } }
        public string Name;
#if !FX11
        private double? howMuch;
        public double? HowMuch { get { return howMuch; } set { howMuch = value; } }
        public bool? HasValue;
#endif
        public override string ToString()
        {
            string s = id + ": " + Name;
#if !FX11
            s += "\t" + howMuch + " / " + HasValue;
#endif
            return s;
        }

    }
    public struct CustomerStruct
    {
        private int id;
        public int Id { get { return id; } set { id = value; } }
        public string Name;
#if !FX11
        private double? howMuch;
        public double? HowMuch { get { return howMuch; } set { howMuch = value; } }
        public bool? HasValue;
#endif
        public override string ToString()
        {
            string s = id + ": " + Name;
#if !FX11
            s += "\t" + howMuch + " / " + HasValue;
#endif
            return s;
        }
    }
}
