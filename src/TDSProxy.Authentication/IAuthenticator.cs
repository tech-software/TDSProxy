using System;
using System.Net;
using JetBrains.Annotations;

namespace TDSProxy.Authentication
{
	[PublicAPI]
	public interface IAuthenticator
	{
		AuthenticationResult Authenticate(IPAddress clientEP, string username, string password, string database);
	}
}
