using System;
using System.ComponentModel;
using System.Net;

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
			if (value is string strValue)
				return IPAddress.Parse(strValue);
			if (value is IPAddress ipAddressValue)
				return ipAddressValue;
			return base.ConvertFrom(context, culture, value);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return typeof(string) == destinationType || destinationType.IsAssignableFrom(typeof(IPAddress)) || base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (null == destinationType)
				throw new ArgumentNullException(nameof(destinationType));
			if (value is IPAddress ipAddress)
			{
				if (typeof(string) == destinationType)
					return ipAddress.ToString();
				if (destinationType.IsAssignableFrom(typeof(IPAddress)))
					return ipAddress;
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
