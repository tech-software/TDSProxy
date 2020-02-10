using System;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public class SSLInvalidPacketException : Exception
	{
		public byte[] PacketData { get; }

		public SSLInvalidPacketException(string message, byte[] packetData, int packetDataLength) : base(message)
		{
			PacketData = new byte[packetDataLength];
			if (packetDataLength > 0)
				Buffer.BlockCopy(packetData, 0, PacketData, 0, packetDataLength);
		}

		public string PacketDataFormatted => PacketData.FormatAsHex();
	}
}
