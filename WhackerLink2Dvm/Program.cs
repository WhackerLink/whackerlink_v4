using System;

namespace WhackerLink2Dvm
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: WhackerLink2Dvm <configPath>");
                return;
            }

            var configPath = args[0];
            var app = new WhackerLink2Dvm(configPath);

            Console.ReadLine();
        }
    }
}