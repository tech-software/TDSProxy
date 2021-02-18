using System;
using System.Configuration;
using JetBrains.Annotations;

namespace TDSProxy.Configuration
{
	public class AuthenticatorElement : ConfigurationElement
	{
		[ConfigurationProperty("name", IsKey = true, IsRequired = true), UsedImplicitly]
		public string Name
		{
			get => (string)base["name"];
			set => base["name"] = value;
		}

		[ConfigurationProperty("dll", IsRequired = true), UsedImplicitly]
		public string Dll
		{
			get => (string)base["dll"];
			set => base["dll"] = value;
		}

		[ConfigurationProperty("class", IsRequired = true), UsedImplicitly]
		public string Class
		{
			get => (string)base["class"];
			set => base["class"] = value;
		}
	}
}
