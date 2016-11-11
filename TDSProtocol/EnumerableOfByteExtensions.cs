using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public static class EnumerableOfByteExtensions
	{
		public static string FormatAsHex(this IEnumerable<byte> bytes)
		{
			if (null == bytes)
				return null;

			if (!bytes.Any())
				return "(no data)";

			StringBuilder sb = new StringBuilder();
			int i = 0;
			foreach (var b in bytes)
			{
				if ((i & 0xf) == 0)
				{
					if (i > 0)
						sb.AppendLine();
					sb.Append(i.ToString("X8")).Append(": ");
				}
				sb.Append(b.ToString("X2")).Append(" ");
				i++;
			}

			return sb.ToString();
		}
	}
}
