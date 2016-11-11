using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProxy.Configuration
{
	public class ListenerCollection : ConfigurationElementCollection
	{
		public ListenerElement this[int index]
		{
			get { return (ListenerElement)base.BaseGet(index); }
			set
			{
				if (null != base.BaseGet(index))
					base.BaseRemoveAt(index);
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
