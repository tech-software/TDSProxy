using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TDSProxy.Configuration
{
	public class IPAddressConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return typeof(string) == sourceType|| typeof(IPAddress).IsAssignableFrom(sourceType) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			var strValue = value as string;
			if (null != strValue)
				return IPAddress.Parse(strValue);
			var ipAddrValue = value as IPAddress;
			if (null != ipAddrValue)
				return ipAddrValue;
			return base.ConvertFrom(context, culture, value);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return typeof(string) == destinationType || destinationType.IsAssignableFrom(typeof(IPAddress)) || base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (null == destinationType)
				throw new ArgumentNullException("destinationType");
			var ipAddr = value as IPAddress;
			if (null != ipAddr)
			{
				if (typeof(string) == destinationType)
					return ipAddr.ToString();
				if (destinationType.IsAssignableFrom(typeof(IPAddress)))
					return ipAddr;
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override bool IsValid(ITypeDescriptorContext context, object value)
		{
			var valueStr = value as string;
			if (null == valueStr)
				return value is IPAddress;
			IPAddress dummy;
			return IPAddress.TryParse(valueStr, out dummy);
		}
	}
}
