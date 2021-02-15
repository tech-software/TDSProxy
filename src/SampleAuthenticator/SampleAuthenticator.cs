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
                AllowConnection = true,
                ConnectToDatabase = database,
                ConnectAsUser = !string.IsNullOrWhiteSpace(username) ? username : "sa",
                DisplayUsername = !string.IsNullOrWhiteSpace(username) ? username : "sa",
                ConnectUsingPassword = !string.IsNullOrWhiteSpace(password) ? password : "YourStrong@Passw0rd",
            };
        }
    }
}
