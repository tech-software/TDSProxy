using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceProcess;

namespace TDSProxy
{
    public sealed partial class TDSProxyService
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

        private static Configuration.TdsProxySection _configuration;
        // ReSharper disable once MemberCanBePrivate.Global
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

        public void Start(string[] args)
        {
            log.InfoFormat(
                "\r\n-----------------\r\nService Starting on {0} with security protocol {1}.\r\n-----------------\r\n",
                AppContext.TargetFrameworkName,
                ServicePointManager.SecurityProtocol);

            if (args.Any(a => string.Equals(a, "debug", StringComparison.OrdinalIgnoreCase)))
            {
                log.Info("Calling Debugger.Break()");
                System.Diagnostics.Debugger.Break();
            }

            VerboseLogging = args.Any(a => string.Equals(a, "verbose", StringComparison.OrdinalIgnoreCase));
            if (VerboseLogging)
				log.Debug("Verbose logging is on.");

            // ReSharper disable once StringLiteralTypo
            VerboseLoggingInWrapper = args.Any(a => string.Equals(a, "wrapperverbose", StringComparison.OrdinalIgnoreCase));
            if (VerboseLoggingInWrapper)
				log.Debug("Verbose logging is on in TDS/SSL wrapper.");

            // ReSharper disable once StringLiteralTypo
            TDSProtocol.TDSPacket.DumpPackets = args.Any(a => string.Equals(a, "packetdump", StringComparison.OrdinalIgnoreCase));
            if (TDSProtocol.TDSPacket.DumpPackets)
				log.Debug("Packet dumping is on.");

            // ReSharper disable once StringLiteralTypo
            SkipLoginProcessing = args.Any(a => string.Equals(a, "skiplogin", StringComparison.OrdinalIgnoreCase));
            if (SkipLoginProcessing)
				log.Debug("Skipping login processing.");

            // ReSharper disable once StringLiteralTypo
            AllowUnencryptedConnections = args.Any(a => string.Equals(a, "allowunencrypted", StringComparison.OrdinalIgnoreCase));
            if (AllowUnencryptedConnections)
				log.Debug("Allowing unencrypted connections (but encryption must be supported because we will not allow unencrypted login).");

            _stopRequested = false;

            StartListeners();

            log.Info("TDSProxyService initialization complete.");
        }

        //new
        public void Stop()
        {
            log.Info("Stopping TDSProxyService");
            LogStats();
            _stopRequested = true;
            StopListeners();
            OnStopping(EventArgs.Empty);
            log.Info("\r\n----------------\r\nService stopped.\r\n----------------\r\n");
        }

        public bool StopRequested => _stopRequested;

        public event EventHandler Stopping;

        private void OnStopping(EventArgs e) => Stopping?.Invoke(this, e);

        private void StartListeners()
        {
            foreach (Configuration.ListenerElement listenerConfig in Configuration.Listeners)
                // ReSharper disable once ObjectCreationAsStatement -- constructed object registers itself
                new TDSListener(this, listenerConfig);
        }

        private void StopListeners()
        {
            List<TDSListener> listeners;
            lock (_listeners)
                listeners = new List<TDSListener>(_listeners);

            // NOTE: listeners de-register themselves
            foreach (var listener in listeners)
                listener.Dispose();
        }

        private void LogStats()
        {
            log.InfoFormat(
                "{0} active connections ({1} connections started since last restart, {2} connections collected without being closed first)",
                TDSConnection.ActiveConnectionCount,
                TDSConnection.TotalConnections,
                TDSConnection.UnclosedCollections);
        }

        internal void AddListener(TDSListener listener)
        {
            lock (_listeners)
                _listeners.Add(listener);
        }

        internal void RemoveListener(TDSListener listener)
        {
            lock (_listeners)
                _listeners.Remove(listener);
        }
    }
}
