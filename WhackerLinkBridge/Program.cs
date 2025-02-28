using System;

namespace WhackerLinkBridge
{
    class Program
    {
        static void Main(string[] args)
        {
            string configPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-c" && i + 1 < args.Length)
                {
                    configPath = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(configPath))
            {
                Console.WriteLine("Usage: WhackerLinkBridge -c <configPath>");
                return;
            }

            var app = new WhackerLinkBridgeApp(configPath);
            app.Start();

            Console.WriteLine("WhackerLinkBridge is running. Press Ctrl+C to exit.");

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
