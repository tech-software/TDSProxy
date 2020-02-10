using System;
using System.Text;

namespace TDSProtocol
{
	public static class Unicode
	{
		public static readonly Encoding Instance = new UnicodeEncoding(false, false, false);
	}
}
