using System;
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;

namespace TDSProxy.Configuration
{
	public class ListenerElement : ConfigurationElement
	{
		private const string ByteValueRegex = @"(?:[01]?[0-9]{1,2}|2(?:[0-4][0-9]|5[0-5]))";
		private const string Ipv4Regex = @"(?:" + ByteValueRegex + @"\.){3}" + ByteValueRegex;
		private const string QuadRegex = @"(?:[0-9a-fA-F]{1,4})";
		private const string ColonQuadRegex = @"(?:\:[0-9a-fA-F]{1,4})";
		private const string QuadColonRegex = @"(?:[0-9a-fA-F]{1,4}:)";
		private const string Ipv6Regex =
			@"(?:" +
			// fully fleshed-out IPv6
			QuadColonRegex + @"{7}" + QuadRegex + @"|" +
			// compressed simple IPv6 with trailing 0s
			QuadColonRegex + @"{0,6}" + QuadRegex + @"?\::|" +
			// compressed simple IPv6 with 1 final non-0 group
			@"(?:" + QuadColonRegex + @"{0,5}" + QuadRegex + @")?\:" + ColonQuadRegex + @"|" +
			// compressed simple IPv6 with 2 final non-0 groups
			@"(?:" + QuadColonRegex + @"{0,4}" + QuadRegex + @")?\:" + ColonQuadRegex + @"{2}|" +
			// compressed simple IPv6 with 3 final non-0 groups
			@"(?:" + QuadColonRegex + @"{0,3}" + QuadRegex + @")?\:" + ColonQuadRegex + @"{3}|" +
			// compressed simple IPv6 with 4 final non-0 groups
			@"(?:" + QuadColonRegex + @"{0,2}" + QuadRegex + @")?\:" + ColonQuadRegex + @"{4}|" +
			// compressed simple IPv6 with 5 final non-0 groups
			@"(?:" + QuadColonRegex + @"?" + QuadRegex + @")?\:" + ColonQuadRegex + @"{5}|" +
			// compressed simple IPv6 with 6 final non-0 groups
			QuadRegex +  @"?\:" + ColonQuadRegex + @"{6}|" +
			// compressed simple IPv6 with 7 final non-0 groups
			@":" + ColonQuadRegex + @"{7}|" +
			// fully fleshed out IPv6 + IPv4
			QuadColonRegex + @"{6}" + Ipv4Regex + @"|" +
			// IPv6 + IPv4 compressed immediately before IPv4 part
			@"(?:" + QuadColonRegex + @"{0,4}" + QuadRegex + @")?\::" + Ipv4Regex + @"|" +
			// IPv6 + IPv4 compressed then 1 group before IPv4 part
			@"(?:" + QuadColonRegex + @"{0,3}" + QuadRegex + @")?\::" + QuadColonRegex + Ipv4Regex + @"|" +
			// IPv6 + IPv4 compressed then 2 groups before IPv4 part
			@"(?:" + QuadColonRegex + @"{0,2}" + QuadRegex + @")?\::" + QuadColonRegex + @"{2}" + Ipv4Regex + @"|" +
			// IPv6 + IPv4 compressed then 3 groups before IPv4 part
			@"(?:" + QuadColonRegex + @"?" + QuadRegex + @")?\::" + QuadColonRegex + @"{3}" + Ipv4Regex + @"|" +
			// IPv6 + IPv4 compressed then 4 groups before IPv4 part
			QuadRegex + @"?\::" + QuadColonRegex + @"{4}" + Ipv4Regex + @"|" +
			// IPv6 + IPv4 compressed then 5 groups before IPv4 part
			@"::" + QuadColonRegex + @"{5}" + Ipv4Regex +
			@")";
		private const string IPAddressRegex = @"(?:" + Ipv4Regex + @"|" + Ipv6Regex + ")";
		private const string HostnameLabel = @"(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)";
		private const string HostnameRegex = @"(?:" + HostnameLabel + @"\.)*" + HostnameLabel;
		private const string HostRegex = "(?:" + HostnameRegex + "|" + IPAddressRegex + ")";
		private const string AnchoredHostRegex = "^" + HostRegex + "$";

		[ConfigurationProperty("name", IsKey = true, IsRequired = true)]
		public string Name
		{
			get => (string)base["name"];
			[UsedImplicitly] set => base["name"] = value;
		}

		[ConfigurationProperty("bindToAddress", IsRequired = false)]
		[TypeConverter(typeof(IPAddressConverter))]
		public IPAddress BindToAddress
		{
			get => (IPAddress)base["bindToAddress"];
			[UsedImplicitly] set => base["bindToAddress"] = value;
		}

		[ConfigurationProperty("listenOnPort", IsRequired = true)]
		public ushort ListenOnPort
		{
			get => (ushort?)base["listenOnPort"] ?? 0;
			[UsedImplicitly] set => base["listenOnPort"] = value;
		}

		[ConfigurationProperty("forwardToHost", IsRequired = true)]
		//[RegexStringValidator(AnchoredHostRegex)]
		public string ForwardToHost
		{
			get => (string)base["forwardToHost"];
			[UsedImplicitly] set => base["forwardToHost"] = value;
		}

		[ConfigurationProperty("forwardToPort", IsRequired = true)]
		public ushort ForwardToPort
		{
			get => (ushort?)base["forwardToPort"] ?? 0;
			[UsedImplicitly] set => base["forwardToPort"] = value;
		}

		[ConfigurationProperty("sslCertStoreName", IsRequired = true)]
		public StoreName SslCertStoreName
		{
			get => (StoreName)base["sslCertStoreName"];
			[UsedImplicitly] set => base["sslCertStoreName"] = value;
		}

		[ConfigurationProperty("sslCertStoreLocation", IsRequired = true)]
		public StoreLocation SslCertStoreLocation
		{
			get => (StoreLocation)base["sslCertStoreLocation"];
			[UsedImplicitly] set => base["sslCertStoreLocation"] = value;
		}

		[ConfigurationProperty("sslCertSubjectThumbprint", IsRequired = true)]
		public string SslCertSubjectThumbprint
		{
			get => (string)base["sslCertSubjectThumbprint"];
			[UsedImplicitly] set => base["sslCertSubjectThumbprint"] = value;
		}

		[ConfigurationProperty("authenticators", IsDefaultCollection = true, IsRequired = true)]
		[ConfigurationCollection(typeof(AuthenticatorCollection))]
		public AuthenticatorCollection Authenticators => (AuthenticatorCollection)base["authenticators"];
	}
}
