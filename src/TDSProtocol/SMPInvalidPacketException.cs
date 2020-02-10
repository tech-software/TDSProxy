using System;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public class SMPInvalidPacketException : Exception
	{
		public byte[] PacketData { get; }

		public SMPInvalidPacketException(string message, byte[] packetData, int packetDataLength)
			: base(message)
		{
			PacketData = new byte[packetDataLength];
			if (packetDataLength > 0)
				Buffer.BlockCopy(packetData, 0, PacketData, 0, packetDataLength);
		}

		public string PacketDataFormatted => PacketData.FormatAsHex();
	}
}
