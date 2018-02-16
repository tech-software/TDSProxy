using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TDSProxy.Authentication;

namespace TDSProxy
{
	class TDSListener : IDisposable
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		readonly TDSProxyService _service;
		readonly TcpListener _tcpListener;
		readonly IPEndPoint _bindToEP;
		readonly IPEndPoint _insideEP;
		readonly CompositionContainer _mefContainer;
		readonly Lazy<IAuthenticator> _export;
		IAuthenticator _authenticator;
		bool _stopRequested;
		bool _stopped;

		internal readonly X509Certificate Certificate;

		public TDSListener(TDSProxyService service, Configuration.ListenerElement configuration)
		{
			var insideAddrs = Dns.GetHostAddresses(configuration.ForwardToHost);
			if (null == insideAddrs || 0 == insideAddrs.Length)
			{
				log.ErrorFormat("Unable to resolve forwardToHost=\"{0}\" for listener {1}", configuration.ForwardToHost, configuration.Name);
				_stopped = true;
				return;
			}
			_insideEP = new IPEndPoint(insideAddrs.First(), configuration.ForwardToPort);

			_service = service;

			_bindToEP = new IPEndPoint(configuration.BindToAddress ?? IPAddress.Any, configuration.ListenOnPort);

			try
			{
				var catalog = new AssemblyCatalog(configuration.AuthenticatorDll);
				_mefContainer = new CompositionContainer(catalog);
				var exports = _mefContainer.GetExports<IAuthenticator>();
				var export = null == exports ? null : exports.FirstOrDefault(a => a.Value.GetType().FullName == configuration.AuthenticatorClass);
				if (null == export)
				{
					log.ErrorFormat(
						"Found dll {0} but not authenticator implementation {1} (DLL exported: {2})",
						configuration.AuthenticatorDll,
						configuration.AuthenticatorClass,
						string.Join("; ", exports.Select(exp => exp.Value.GetType().FullName)),
						null == exports
							? 0
							: exports.Count());
					Dispose();
					return;
				}
				_export = export;
				_authenticator = _export.Value;
				_mefContainer.ReleaseExports(exports.Where(e => e != _export));
			}
			catch (CompositionException ce)
			{
				log.Error(
					"Failed to find an authenticator. Composition errors:\r\n\t" +
					string.Join("\r\n\t", ce.Errors.Select(err => "Element: " + err.Element.DisplayName + ", Error: " + err.Description)),
					ce);
				Dispose();
				return;
			}
			catch (Exception e)
			{
				log.Error("Failed to find an authenticator", e);
				Dispose();
				return;
			}

			try
			{
				log.DebugFormat("Opening SSL certificate store {0}.{1}", configuration.SslCertStoreLocation, configuration.SslCertStoreName);
				var store = new X509Store(configuration.SslCertStoreName, configuration.SslCertStoreLocation);
				store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
				var matching = store.Certificates.Find(X509FindType.FindByThumbprint, configuration.SslCertSubjectThumbprint, false);
				if (null == matching || 0 == matching.Count)
				{
					log.ErrorFormat(
						"Failed to find SSL certification with thumbprint '{0}' in location {1}, store {2}.",
						configuration.SslCertSubjectThumbprint,
						configuration.SslCertStoreLocation,
						configuration.SslCertStoreName);
					Dispose();
					return;
				}
				Certificate = matching[0];
			}
			catch (Exception e)
			{
				log.Error("Failed to load SSL certificate", e);
				Dispose();
				return;
			}

			_tcpListener = new TcpListener(_bindToEP);
			_tcpListener.Start();
			_tcpListener.BeginAcceptTcpClient(AcceptConnection, _tcpListener);

			_service.AddListener(this);

			log.InfoFormat(
				"Listening on {0} and forwarding to {1} (SSL cert DN {2}; serial {3}; authenticator {4})",
				_bindToEP,
				_insideEP,
				Certificate.Subject,
				Certificate.GetSerialNumberString(),
				_authenticator.GetType().FullName);
		}

		public IAuthenticator Authenticator
		{
			get { return _authenticator; }
		}

		public IPEndPoint ForwardTo
		{
			get { return _insideEP; }
		}

		private void service_Stopping(object sender, EventArgs e)
		{
			log.DebugFormat("Stopping listener on {0}", _insideEP);
			Dispose();
		}

		private void AcceptConnection(IAsyncResult result)
		{
			try
			{
				// Get connection
				TcpClient readClient = ((TcpListener)result.AsyncState).EndAcceptTcpClient(result);
				log.DebugFormat("Accepted connection from {0} on {1}, will forward to {2}", readClient.Client.RemoteEndPoint, readClient.Client.LocalEndPoint, _insideEP);

				// Handle stop requested
				if (_stopRequested)
				{
					readClient.Close();
					return;
				}

				// Listen for next connection
				_tcpListener.BeginAcceptTcpClient(AcceptConnection, _tcpListener);

				// Process this connection
				new TDSConnection(_service, this, readClient, _insideEP);
			}
			catch (ObjectDisposedException) { /* We're shutting down, ignore */ }
			catch (Exception e)
			{
				log.Fatal("Error in AcceptConnection, won't resume listening.", e);
			}
		}

		public void Dispose()
		{
			if (!_stopped)
			{
				_stopped = true;
				_service.RemoveListener(this);
				_tcpListener.Stop();
				_authenticator = null;
				if (null != _mefContainer)
				{
					if (null != _export)
						_mefContainer.ReleaseExport(_export);
					_mefContainer.Dispose();
				}
			}
		}
	}
}
