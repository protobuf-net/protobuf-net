using Xunit;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Xunit.Abstractions;

namespace Examples.Issues
{
    public class Issue167
    {
        private ITestOutputHelper Log { get; }
        public Issue167(ITestOutputHelper _log) => Log = _log;

        [Fact]
        public void Execute()
        {
            var test = new Problematic();
            try
            {
                using (var memStream = new MemoryStream())
                {
                    Serializer.Serialize<Problematic>(memStream, test); //causes stackoverflow exception.
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.StackTrace);
            }
        }

        [ProtoContract]
        class Problematic : IEnumerable
        {
            private List<Problematic> _children =
                new List<Problematic>();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _children.GetEnumerator();
            }

            public Problematic this[int i]
            {
                get { return _children[i]; }
            }
        }
    }
}
