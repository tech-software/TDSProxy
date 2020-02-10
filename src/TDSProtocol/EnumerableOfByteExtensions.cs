using System;
using System.Collections.Generic;
using System.Text;

namespace TDSProtocol
{
	public static class EnumerableOfByteExtensions
	{
		public static string FormatAsHex(this IEnumerable<byte> bytes, string prefix = null)
		{
			if (null == bytes)
				return null;

			using (var enumerator = bytes.GetEnumerator())
			{
				if (!enumerator.MoveNext())
					return "(no data)";

				StringBuilder sb = new StringBuilder();
				bool readByte = true;
				uint i = 0;
				while (readByte)
				{
					if (i > 0) sb.AppendLine();
					sb.Append(prefix).Append(i.ToString("X8")).Append(":");

					for (var j = 0; readByte && j < 16; j++, readByte = enumerator.MoveNext())
					{
						sb.Append(" ").Append(enumerator.Current.ToString("X2"));
					}

					i += 16;
				}

				return sb.ToString();
			}
		}
	}
}
