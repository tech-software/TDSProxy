using System;

namespace TDSProtocol
{
	public class SSLInvalidPacketException : Exception
	{
		private readonly byte[] _packetData;

		public byte[] PacketData
		{
			get { return _packetData; }
		}

		public SSLInvalidPacketException(string message, byte[] packetData, int packetDataLength) : base(message)
		{
			_packetData = new byte[packetDataLength];
			if (packetDataLength > 0)
				Buffer.BlockCopy(packetData, 0, _packetData, 0, packetDataLength);
		}

		public string PacketDataFormatted
		{
			get { return _packetData.FormatAsHex(); }
		}
	}
}
