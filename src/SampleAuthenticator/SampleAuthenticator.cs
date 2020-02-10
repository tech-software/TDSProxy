using System;
using System.Net;
using System.ComponentModel.Composition;

using TDSProxy.Authentication;

namespace SampleAuthenticator
{
	[Export(typeof(IAuthenticator))]
	public class SampleAuthenticator : IAuthenticator
	{
		public AuthenticationResult Authenticate(IPAddress clientIp, string username, string password, string database)
		{
			return new AuthenticationResult
			{
				// ReSharper disable once StringLiteralTypo
				AllowConnection = !string.Equals(username, "baduser", StringComparison.OrdinalIgnoreCase),
				ConnectToDatabase = database,
				ConnectAsUser = username,
				ConnectUsingPassword = password,
				DisplayUsername = username
			};
		}
	}
}
