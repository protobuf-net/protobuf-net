using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Examples.DesignIdeas
{
/*  Given two wire-compatible types from different sources
 *  (say, the same data from two different imports - such
 *  as a web-proxy vs a local assembly), allow simple
 *  translation between them;
 *  
 *  Also to allow painless promotion / demotion through object hierarchies
 */

    [ProtoContract]
    public sealed class Foo {
        [ProtoMember(1)]
        public int A { get; set; }
    }
    [ProtoContract]
    public class Bar {
        [ProtoMember(1)]
        public int B { get; set; }
    }
    [ProtoContract]
    public sealed class SubBar : Bar
    {
        [ProtoMember(2)]
        public int C { get; set; }
    }

    static class ChangeTypeThoughts {
        static void Idea() {
            Foo foo = new Foo { A = 98 };
            Bar bar = Serializer.ChangeType<Foo, Bar>(foo);

            Console.WriteLine(foo.A == bar.B);

            SubBar subBar = Serializer.ChangeType<Bar, SubBar>(bar);

            Console.WriteLine(subBar.B == bar.B);

            Bar bar2 = Serializer.ChangeType<SubBar, Bar>(subBar);

            Console.WriteLine(bar.B == bar2.B);
        }
    }
}
