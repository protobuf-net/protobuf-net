using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Examples
{
    class Program
    {
        static void Main()
        {
            #region Demo Runner
            int index = 0;
            Action<Action<int>> run = demo =>
            {
                index++;
                try
                {
                    Console.WriteLine("[demo {0}; {1}]", index,
                        demo.Method.DeclaringType.Name);
                    Console.WriteLine();
                    demo(index);
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("!!!!! Exception in test {0}", index);
                    Console.WriteLine("\t" + ex.GetType().Name);
                    Console.WriteLine("\t" + ex.Message);
                    Console.WriteLine();
                }
                finally
                {
                    Console.WriteLine();
                    Console.WriteLine("[end demo {0}]", index);
                    Console.WriteLine();
                }
            };
            #endregion

            run(SimpleStream.SimpleStreamDemo.Run);
            run(Remoting.RemotingDemo.Run);

            
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
        static void RunDemo(Action<int> demo)
        {

        }
    }
}
