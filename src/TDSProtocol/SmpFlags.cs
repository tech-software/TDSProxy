using System;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public enum SmpFlags : byte
	{
		Syn = 0x01,
		Ack = 0x02,
		Fin = 0x04,
		Data = 0x08
	}
}
