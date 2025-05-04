using System;

namespace WhackerLink2Dvm
{
    class Program
    {
        static void Main(string[] args)
        {
            string configPath = "config.yml";

            if (args.Length != 0)
                configPath = args[0];
            
            WhackerLink2Dvm app = new WhackerLink2Dvm(configPath);

            Console.ReadLine();
        }
    }
}