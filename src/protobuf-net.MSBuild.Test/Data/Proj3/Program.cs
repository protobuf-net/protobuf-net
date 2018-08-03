using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        var x = TestNS.Foo.A;
        var y = TestNS.Bar.E;

        var z = new TestNS.EnumFirstIsNonZero();
    }
}
