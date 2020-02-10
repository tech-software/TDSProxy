using System;

namespace TDSProtocol
{
	public static class StringExtensions
	{
		public static int UnicodeByteLength(this string theString)
		{
			if (string.IsNullOrEmpty(theString))
				return 0;
			return Unicode.Instance.GetByteCount(theString);
		}
	}
}
