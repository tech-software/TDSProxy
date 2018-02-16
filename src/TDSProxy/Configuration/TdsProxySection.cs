using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProxy.Configuration
{
	public class TdsProxySection : ConfigurationSection
	{
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[ConfigurationProperty("xmlns")]
		public string Xmlns
		{
			get { return (string)base["xmlns"]; }
			set { base["xmlns"] = value; }
		}

		[ConfigurationProperty("listeners", IsDefaultCollection = true, IsRequired = true)]
		[ConfigurationCollection(typeof(ListenerCollection))]
		public ListenerCollection Listeners
		{
			get { return (ListenerCollection)base["listeners"]; }
		}
	}
}
