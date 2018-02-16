using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
				AllowConnection = !string.Equals(username, "baduser", StringComparison.OrdinalIgnoreCase),
				ConnectToDatabase = database,
				ConnectAsUser = username,
				ConnectUsingPassword = password,
				DisplayUsername = username
			};
		}
	}
}
