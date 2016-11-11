using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TDSProxy
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[] 
			{ 
				new TDSProxyService()
			};
			ServiceBase.Run(ServicesToRun);
		}
	}
}
