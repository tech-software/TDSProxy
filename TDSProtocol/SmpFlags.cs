using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public enum SmpFlags : byte
	{
		Syn = 0x01,
		Ack = 0x02,
		Fin = 0x04,
		Data = 0x08
	}
}
