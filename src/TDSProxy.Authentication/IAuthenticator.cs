using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TDSProxy.Authentication
{
	public interface IAuthenticator
	{
		AuthenticationResult Authenticate(IPAddress clientEP, string username, string password, string database);
	}
}
