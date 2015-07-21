using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SymbolBinder;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[1];
            args[0] = @"Y:\Projects\Practice\NugetConsumer\packages\Cdemo.1.0.0.0";
#endif
            long time = CodeTimer.Time("MDBG", 1, () =>
            {
                Binder.Execute(args[0]);
            });
            Console.WriteLine(time);
            Console.ReadKey();
        }
    }
}
