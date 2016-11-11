using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TDSProxy.Configuration
{
	public class ListenerElement : ConfigurationElement
	{
		const string _ipv4Regex = @"(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])))";
		const string _ipv6Regex =
			// fully fleshed-out IPv6
			@"(([0-9a-fA-F]{1,4}:){7}([0-9a-fA-F]{1,4}))|" +
			// compressed simple IPv6 with trailing 0s
			@"((([0-9a-fA-F]{1,4}:){0,6}([0-9a-fA-F]{1,4}))?::)|" +
			// compressed simple IPv6 with 1 final non-0 group
			@"((([0-9a-fA-F]{1,4}:){0,5}([0-9a-fA-F]{1,4}))?:(:[0-9a-fA-F]{1,4}))|" +
			// compressed simple IPv6 with 2 final non-0 groups
			@"((([0-9a-fA-F]{1,4}:){0,4}([0-9a-fA-F]{1,4}))?:(:[0-9a-fA-F]{1,4}){2})|" +
			// compressed simple IPv6 with 3 final non-0 groups
			@"((([0-9a-fA-F]{1,4}:){0,3}([0-9a-fA-F]{1,4}))?:(:[0-9a-fA-F]{1,4}){3})|" +
			// compressed simple IPv6 with 4 final non-0 groups
			@"((([0-9a-fA-F]{1,4}:){0,2}([0-9a-fA-F]{1,4}))?:(:[0-9a-fA-F]{1,4}){4})|" +
			// compressed simple IPv6 with 5 final non-0 groups
			@"((([0-9a-fA-F]{1,4}:){0,1}([0-9a-fA-F]{1,4}))?:(:[0-9a-fA-F]{1,4}){5})|" +
			// compressed simple IPv6 with 6 final non-0 groups
			@"([0-9a-fA-F]{0,4}:(:[0-9a-fA-F]{1,4}){6})|" +
			// compressed simple IPv6 with 7 final non-0 groups
			@"(:(:[0-9a-fA-F]{1,4}){7})|" +
			// fully fleshed out IPv6 + IPv4
			@"(([0-9a-fA-F]{1,4}:){6}(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))))|" +
			// IPv6 + IPv4 compressed immediately before IPv4 part
			@"((([0-9a-fA-F]{1,4}:){0,4}[0-9a-fA-F]{1,4})?::(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))))|" +
			// IPv6 + IPv4 compressed then 1 group before IPv4 part
			@"((([0-9a-fA-F]{1,4}:){0,3}[0-9a-fA-F]{1,4})?::[0-9a-fA-F]{1,4}:(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))))|" +
			// IPv6 + IPv4 compressed then 2 groups before IPv4 part
			@"((([0-9a-fA-F]{1,4}:){0,2}[0-9a-fA-F]{1,4})?::([0-9a-fA-F]{1,4}:){2}(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))))|" +
			// IPv6 + IPv4 compressed then 3 groups before IPv4 part
			@"((([0-9a-fA-F]{1,4}:){0,1}[0-9a-fA-F]{1,4})?::([0-9a-fA-F]{1,4}:){3}(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))))|" +
			// IPv6 + IPv4 compressed then 4 groups before IPv4 part
			@"([0-9a-fA-F]{0,4}::([0-9a-fA-F]{1,4}:){4}(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))))|" +
			// IPv6 + IPv4 compressed then 5 groups before IPv4 part
			@"(::([0-9a-fA-F]{1,4}:){5}(([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5])\.){3}([01]?[0-9]?[0-9]|2([0-4][0-9]|5[0-5]))))";
		const string ipAddressRegex = "(" + _ipv4Regex + ")|(" + _ipv6Regex + ")";
		const string hostnameRegex = @"(?!-)(?!.{256})[a-zA-Z0-9-]{1,63}(\.[a-zA-Z0-9-]{1,63})*\.?";
		const string hostRegex = "(" + hostnameRegex + ")|" + ipAddressRegex;

		[ConfigurationProperty("name", IsKey = true, IsRequired = true)]
		public string Name
		{
			get { return (string)base["name"]; }
			set { base["name"] = value; }
		}

		[ConfigurationProperty("bindToAddress", IsRequired = false)]
		[TypeConverter(typeof(IPAddressConverter))]
		public IPAddress BindToAddress
		{
			get { return (IPAddress)base["bindToAddress"]; }
			set { base["bindToAddress"] = value; }
		}

		[ConfigurationProperty("listenOnPort", IsRequired = true)]
		public ushort ListenOnPort
		{
			get { return (ushort?)base["listenOnPort"] ?? 0; }
			set { base["listenOnPort"] = value; }
		}

		[ConfigurationProperty("forwardToHost", IsRequired = true)]
		//[RegexStringValidator(hostRegex)]
		public string ForwardToHost
		{
			get { return (string)base["forwardToHost"]; }
			set { base["forwardToHost"] = value; }
		}

		[ConfigurationProperty("forwardToPort", IsRequired = true)]
		public ushort ForwardToPort
		{
			get { return (ushort?)base["forwardToPort"] ?? 0; }
			set { base["forwardToPort"] = value; }
		}

		[ConfigurationProperty("sslCertStoreName", IsRequired = true)]
		public StoreName SslCertStoreName
		{
			get { return (StoreName)base["sslCertStoreName"]; }
			set { base["sslCertStoreName"] = value; }
		}

		[ConfigurationProperty("sslCertStoreLocation", IsRequired = true)]
		public StoreLocation SslCertStoreLocation
		{
			get { return (StoreLocation)base["sslCertStoreLocation"]; }
			set { base["sslCertStoreLocation"] = value; }
		}

		[ConfigurationProperty("sslCertSubjectThumbprint", IsRequired = true)]
		public string SslCertSubjectThumbprint
		{
			get { return (string)base["sslCertSubjectThumbprint"]; }
			set { base["sslCertSubjectThumbprint"] = value; }
		}

		[ConfigurationProperty("authenticatorDll", IsRequired = true)]
		public string AuthenticatorDll
		{
			get { return (string)base["authenticatorDll"]; }
			set { base["authenticatorDll"] = value; }
		}

		[ConfigurationProperty("authenticatorClass", IsRequired = true)]
		public string AuthenticatorClass
		{
			get { return (string)base["authenticatorClass"]; }
			set { base["authenticatorClass"] = value; }
		}
	}
}
