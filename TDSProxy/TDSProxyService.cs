using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using TDSProxy.Authentication;

namespace TDSProxy
{
	public partial class TDSProxyService : ServiceBase
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		public static bool VerboseLogging { get; private set; }
		public static bool VerboseLoggingInWrapper { get; private set; }
		public static bool SkipLoginProcessing { get; private set; }
		public static bool AllowUnencryptedConnections { get; private set; }

		private readonly HashSet<TDSListener> _listeners = new HashSet<TDSListener>();

		private bool _stopRequested;

		private static Configuration.TdsProxySection _configuration = null;
		public static Configuration.TdsProxySection Configuration
		{
			get 
			{
				if (null == _configuration)
					try
					{
						_configuration = (Configuration.TdsProxySection)ConfigurationManager.GetSection("tdsProxy");
					}
					catch (Exception e)
					{
						log.Error("Error reading configuration", e);
						throw;
					}
				return _configuration;
			}
		}

		public TDSProxyService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			log.Info("\r\n-----------------\r\nService Starting.\r\n-----------------\r\n");

			if (args.Any(a => string.Equals(a, "debug", StringComparison.OrdinalIgnoreCase)))
			{
				log.Info("Calling Debugger.Break()");
				System.Diagnostics.Debugger.Break();
			}

			VerboseLogging = args.Any(a => string.Equals(a, "verbose", StringComparison.OrdinalIgnoreCase));
			if (VerboseLogging)
				log.Debug("Verbose logging is on.");

			VerboseLoggingInWrapper = args.Any(a => string.Equals(a, "wrapperverbose", StringComparison.OrdinalIgnoreCase));
			if (VerboseLoggingInWrapper)
				log.Debug("Verbose logging is on in TDS/SSL wrapper.");

			TDSProtocol.TDSPacket.DumpPackets = args.Any(a => string.Equals(a, "packetdump", StringComparison.OrdinalIgnoreCase));
			if (TDSProtocol.TDSPacket.DumpPackets)
				log.Debug("Packet dumping is on.");

			SkipLoginProcessing = args.Any(a => string.Equals(a, "skiplogin", StringComparison.OrdinalIgnoreCase));
			if (SkipLoginProcessing)
				log.Debug("Skipping login processing.");

			AllowUnencryptedConnections = args.Any(a => string.Equals(a, "allowunencrypted", StringComparison.OrdinalIgnoreCase));
			if (AllowUnencryptedConnections)
				log.Debug("Allowing unencrypted connections (but encryption must be supported because we will not allow unencryption login).");

			_stopRequested = false;

			StartListeners();

			log.Info("TDSProxyService initialization complete.");
		}

		protected override void OnStop()
		{
			log.Info("Stopping TDSProxyService");
			LogStats();
			_stopRequested = true;
			StopListeners();
			OnStopping(EventArgs.Empty);
			log.Info("\r\n----------------\r\nService stopped.\r\n----------------\r\n");
		}

		protected override void OnPause()
		{
			StopListeners();
			log.Info("Service paused.");
		}

		protected override void OnContinue()
		{
			log.Info("Resuming service.");
			RefreshConfiguration();
			StartListeners();
		}

		protected override void OnCustomCommand(int command)
		{
			if (_stopRequested)
				return;

			switch (command)
			{
			case 200:
				LogStats();
				break;
			case 201:
				StopListeners();
				RefreshConfiguration();
				StartListeners();
				break;
			}
		}

		public event EventHandler Stopping;

		protected virtual void OnStopping(EventArgs e)
		{
			var stopping = Stopping;
			if (null != stopping)
				stopping(this, e);
		}

		private void StartListeners()
		{
			foreach (Configuration.ListenerElement listenerConfig in Configuration.Listeners)
				new TDSListener(this, listenerConfig);
		}

		private void StopListeners()
		{
			var listeners = new List<TDSListener>(_listeners);
			foreach (var listener in listeners)
				listener.Dispose();
		}

		private void RefreshConfiguration()
		{
			ConfigurationManager.RefreshSection("tdsProxy");
			_configuration = null;
		}

		private void LogStats()
		{
			log.InfoFormat(
				"{0} active connections ({1} connections started since last restart, {2} connections collected without being closed first)",
				TDSConnection._activeConnectionCount,
				TDSConnection._totalConnections,
				TDSConnection._unclosedCollections);
		}

		internal void AddListener(TDSListener listener)
		{
			lock(_listeners)
				_listeners.Add(listener);
		}

		internal void RemoveListener(TDSListener listener)
		{
			lock (_listeners)
				_listeners.Remove(listener);
		}
	}
}
