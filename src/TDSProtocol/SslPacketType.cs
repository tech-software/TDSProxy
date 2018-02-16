using System;

namespace TDSProtocol
{
	public enum SslPacketType : byte
	{
		ChangeCipherSpec = 20,
		Alert = 21,
		Handshake = 22,
		ApplicationData = 23
	}
}
