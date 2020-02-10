using System;
using System.ComponentModel;
using System.Configuration;
using JetBrains.Annotations;

namespace TDSProxy.Configuration
{
	public class TdsProxySection : ConfigurationSection
	{
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[ConfigurationProperty("xmlns")]
		[UsedImplicitly]
		public string Xmlns
		{
			get => (string)base["xmlns"];
			set => base["xmlns"] = value;
		}

		[ConfigurationProperty("listeners", IsDefaultCollection = true, IsRequired = true)]
		[ConfigurationCollection(typeof(ListenerCollection))]
		public ListenerCollection Listeners => (ListenerCollection)base["listeners"];
	}
}
