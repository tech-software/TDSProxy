using System;
using System.ServiceProcess;

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
			if (Environment.UserInteractive)
			{
				service.Start(args);
				Console.Write("Press ESC to end...");
				while (Console.ReadKey(false).Key != ConsoleKey.Escape) {}
				service.Stop();
			}
			else
			{
				ServiceBase.Run(new TDSProxyService());
			}
		}
	}
}
