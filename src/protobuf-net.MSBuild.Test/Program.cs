using Microsoft.Build.Locator;
using System;
using Xunit.Abstractions;

namespace ProtoBuf.Build
{
    class ConsoleOutput : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
        }
    }
}
