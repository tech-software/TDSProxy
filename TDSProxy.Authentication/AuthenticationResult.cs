using System;

namespace TDSProxy.Authentication
{
	public class AuthenticationResult
	{
		public AuthenticationResult() { }
		public AuthenticationResult(AuthenticationResult toCopy)
		{
			AllowConnection = toCopy.AllowConnection;
			DisplayUsername = toCopy.DisplayUsername;
			ConnectToDatabase = toCopy.ConnectToDatabase;
			ConnectAsUser = toCopy.ConnectAsUser;
			ConnectUsingPassword = toCopy.ConnectUsingPassword;
		}

		public bool AllowConnection { get; set; }
		public string DisplayUsername { get; set; }
		public string ConnectToDatabase { get; set; }
		public string ConnectAsUser { get; set; }
		public string ConnectUsingPassword { get; set; }
	}
}
