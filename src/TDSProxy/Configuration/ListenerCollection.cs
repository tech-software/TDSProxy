using System;
using System.Configuration;

namespace TDSProxy.Configuration
{
	public class ListenerCollection : ConfigurationElementCollection
	{
		// ReSharper disable once UnusedMember.Global
		public ListenerElement this[int index]
		{
			get => (ListenerElement)BaseGet(index);
			set
			{
				if (null != BaseGet(index))
					BaseRemoveAt(index);
				base.BaseAdd(index, value);
			}
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new ListenerElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ListenerElement)element).Name;
		}
	}
}
