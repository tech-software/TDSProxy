using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public static class Unicode
	{
		public static readonly Encoding Instance = new UnicodeEncoding(false, false, false);
	}
}
