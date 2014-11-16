using CodeModel;
using System;
using System.IO;
using System.Linq;

namespace ConsoleGen
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                return PrintUsage("Not enough parameters!");
            }
            var assemblies = Utils.TakeAllButLast(args);
            var textPath = args.Last();
            using(var writer = File.CreateText(textPath))
            {
                var scanner = new CecilScanner.Scanner();
                var api = scanner.ScanApi(assemblies);
                var generator = new AngularGenerator(api, writer);
                generator.Run();
            }
            return 0;
        }

        private static int PrintUsage(string p)
        {
            Console.WriteLine(p);
            Console.WriteLine(
@"Usage:
ModelsGen AssemblyName [AssemblyName..] OutputFile.ts
");
            return 1;
        }
    }
}
