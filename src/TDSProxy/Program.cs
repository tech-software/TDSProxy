using System;

namespace TDSProxy
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var service = new TDSProxyService();
            service.Start(args);
            Console.Write("Press ESC to end...");
            while (Console.ReadKey(false).Key != ConsoleKey.Escape) { }
            service.Stop();
        }
    }
}
