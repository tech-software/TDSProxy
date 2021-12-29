using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using TDSProxy.Authentication;
using TDSProxy.Configuration;

namespace TDSProxy
{
	class TDSListener : IDisposable
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		readonly TDSProxyService _service;
		readonly TcpListener _tcpListener;
		readonly CompositionContainer _mefContainer;
		volatile bool _stopped;

		internal readonly X509Certificate Certificate;

		public TDSListener(TDSProxyService service, ListenerElement configuration)
		{
			var insideAddresses = Dns.GetHostAddresses(configuration.ForwardToHost);
			if (0 == insideAddresses.Length)
			{
				log.ErrorFormat("Unable to resolve forwardToHost=\"{0}\" for listener {1}", configuration.ForwardToHost, configuration.Name);
				_stopped = true;
				return;
			}
			ForwardTo = new IPEndPoint(insideAddresses.First(), configuration.ForwardToPort);

			_service = service;

			var bindToEP = new IPEndPoint(configuration.BindToAddress ?? IPAddress.Any, configuration.ListenOnPort);

			try
			{
				var catalog = new AggregateCatalog(from AuthenticatorElement a in configuration.Authenticators
				                                   select new AssemblyCatalog(a.Dll));
				_mefContainer = new CompositionContainer(catalog);

                _authenticators = _mefContainer.GetExports<IAuthenticator>().ToArray();
                if (!_authenticators.Any())
                {
                    throw new InvalidOperationException("No authenticators");
                }
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
				if (0 == matching.Count)
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

			_tcpListener = new TcpListener(bindToEP);
			_tcpListener.Start();
			_tcpListener.BeginAcceptTcpClient(AcceptConnection, _tcpListener);

			_service.AddListener(this);

			log.InfoFormat(
				"Listening on {0} and forwarding to {1} (SSL cert DN {2}; expires {5} serial {3}; authenticators {4})",
				bindToEP,
				ForwardTo,
				Certificate.Subject,
				Certificate.GetSerialNumberString(),
				string.Join(", ", from a in Authenticators select a.GetType().FullName),
				Certificate.GetExpirationDateString());
		}

		//public IAuthenticator Authenticator { get; private set; }
		private Lazy<IAuthenticator>[] _authenticators;

		public IEnumerable<Lazy<IAuthenticator>> Authenticators =>
			!_stopped
				? (Lazy<IAuthenticator>[])_authenticators.Clone()
				: throw new ObjectDisposedException(nameof(TDSListener));

		// ReSharper disable once MemberCanBePrivate.Global
		public IPEndPoint ForwardTo { get; }

		private void AcceptConnection(IAsyncResult result)
		{
			try
			{
				// Get connection
				TcpClient readClient = ((TcpListener)result.AsyncState).EndAcceptTcpClient(result);

				//Log as Info so we have the open (and the close elsewhere)
				log.InfoFormat("Accepted connection from {0} on {1}, will forward to {2}", readClient.Client.RemoteEndPoint, readClient.Client.LocalEndPoint, ForwardTo);

				// Handle stop requested
				if (_service?.StopRequested == true)
				{
					log.Info("Service was ending, closing connection and returning.");
					readClient.Close();
					return;
				}

				// Process this connection
				// ReSharper disable once ObjectCreationAsStatement -- constructed object registers itself
				new TDSConnection(_service, this, readClient, ForwardTo);
			}
			catch (ObjectDisposedException) { /* We're shutting down, ignore */ }
			catch (Exception e)
			{
				log.Fatal("Error in AcceptConnection.", e);
			}

            // Listen for next connection -- Do this here so we accept new connections even if this attempt to accept failed.
            _tcpListener.BeginAcceptTcpClient(AcceptConnection, _tcpListener);
		}

		public void Dispose()
		{
			if (!_stopped)
			{
				_stopped = true;
				_service?.RemoveListener(this);
				_tcpListener?.Stop();
				if (null != _mefContainer)
				{
					if (null != _authenticators)
						_mefContainer.ReleaseExports(_authenticators);
					_mefContainer.Dispose();
				}
				_authenticators = null;
			}
		}
	}
}
