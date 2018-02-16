using System;

namespace TDSProtocol
{
	[Flags]
	public enum TDSStatus : byte
	{
		Normal = 0,
		EndOfMessage = 0x01,
		Ignore = 0x02,
		ResetConnection = 0x08,
		ResetConnectionSkipTran = 0x10
	}
}
