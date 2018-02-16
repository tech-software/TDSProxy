using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public class TDSInvalidPacketException : Exception
	{
		private readonly byte[] _packetData;

		public byte[] PacketData
		{
			get { return _packetData; }
		}

		public TDSInvalidPacketException(string message, byte[] packetData, int packetDataLength) : base(message)
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
