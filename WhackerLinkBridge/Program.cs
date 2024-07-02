using System;

namespace WhackerLinkBridge
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: WhackerLinkBridge <configPath>");
                return;
            }

            var configPath = args[0];
            var app = new WhackerLinkBridgeApp(configPath);
            app.Start();

            Console.WriteLine("WhackerLinkBridge is running");
            Console.ReadLine();
        }
    }
}