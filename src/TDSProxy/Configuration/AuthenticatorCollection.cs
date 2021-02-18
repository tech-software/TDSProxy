using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TDSProxy.Configuration
{
	public class AuthenticatorCollection : ConfigurationElementCollection
	{
		[UsedImplicitly]
		public AuthenticatorElement this[int index]
		{
			get => (AuthenticatorElement)BaseGet(index);
			set
			{
				if (null != BaseGet(index)) BaseRemoveAt(index);
				BaseAdd(index, value);
			}
		}

		protected override ConfigurationElement CreateNewElement() => new AuthenticatorElement();

		protected override object GetElementKey(ConfigurationElement element) => ((AuthenticatorElement)element).Name;
	}
}
