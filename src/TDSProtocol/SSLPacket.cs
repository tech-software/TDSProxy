using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public class SSLPacket
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		protected const int HeaderLength = 5;

		private static readonly Dictionary<int, SslProtocols> ProtocolVersionNumbers = new Dictionary<int, SslProtocols>
		{
			{ 0x0200, SslProtocols.Ssl2 },
			{ 0x0300, SslProtocols.Ssl3 },
			{ 0x0301, SslProtocols.Tls },
			{ 0x0302, SslProtocols.Tls11 },
			{ 0x0303, SslProtocols.Tls12 }
		};

		private static readonly HashSet<SslPacketType> KnownPacketTypes =
			new HashSet<SslPacketType>(Enum.GetValues(typeof(SslPacketType)).Cast<SslPacketType>());
		public static bool IsSSLPacketType(byte packetTypeByte)
		{
			return KnownPacketTypes.Contains((SslPacketType)packetTypeByte);
		}

		internal readonly byte[] Data;

		protected SSLPacket(byte[] data)
		{
			if (null == data)
				throw new ArgumentNullException(nameof(data));
			if (data.Length < HeaderLength || data.Length > ushort.MaxValue + HeaderLength)
				throw new ArgumentOutOfRangeException(nameof(data), "data must be at least " + HeaderLength + " bytes and no more than " + (ushort.MaxValue + HeaderLength) + " bytes long.");
			if (!IsSSLPacketType(data[0]))
				throw new ArgumentOutOfRangeException(nameof(data), "Unrecognized SSL packet type " + data[0]);
			if (HeaderLength + GetPayloadLength(data) != data.Length)
				throw new ArgumentOutOfRangeException(nameof(data), "Payload length in data does not match length of data.");
			if (!ProtocolVersionNumbers.ContainsKey((data[1] << 8) | data[2]))
				throw new ArgumentOutOfRangeException(nameof(data), "Unrecognized SSL protocol version " + data[1] + "." + data[2] + ".");
			Data = data;
		}

		public SslPacketType PacketType => (SslPacketType)Data[0];

		public SslProtocols SslProtocol => ProtocolVersionNumbers[(Data[1] << 8) | Data[2]];

		public ushort PayloadLength => GetPayloadLength(Data);

		private static ushort GetPayloadLength(byte[] data)
		{
			return unchecked((ushort)((data[3] << 8) | data[4]));
		}

		public byte[] Payload
		{
			get
			{
				var pl = new byte[PayloadLength];
				Buffer.BlockCopy(Data, HeaderLength, pl, 0, pl.Length);
				return pl;
			}
		}

		public static async Task<SSLPacket> ReadFromStreamAsync(Stream stream, bool readPacketType, SslPacketType? packetType = null)
		{
			if ((!readPacketType) && (!packetType.HasValue))
				throw new ArgumentException("packetType must be specified if readPacketType is false.");

			var header = new byte[HeaderLength];
			int headerBytesRead;

			if (readPacketType)
			{
				headerBytesRead = await stream.ReadAsync(header, 0, 1).ConfigureAwait(false);
				if (headerBytesRead == 0)
					return null;

				if (!IsSSLPacketType(header[0]))
					throw new SSLInvalidPacketException("Packet type " + header[0].ToString("X2") + " is not a recognized SSL/TLS packet type.", header, 1);
			}
			else
			{
				header[0] = (byte)packetType.Value;
				headerBytesRead = 1;
			}
			if (packetType.HasValue && (byte)packetType.Value != header[0])
				throw new SSLInvalidPacketException("Packet type " + (SslPacketType)header[0] + " does not match specified value of " + packetType.Value + ".", header, 1);

			while (headerBytesRead < HeaderLength)
			{
				var thisRead = await stream.ReadAsync(header, headerBytesRead, HeaderLength - headerBytesRead).ConfigureAwait(false);
				if (thisRead == 0)
					throw new SSLInvalidPacketException("Stream was closed mid-header", header, headerBytesRead);
				headerBytesRead += thisRead;
			}

			var payloadLength = GetPayloadLength(header);
			var data = new byte[HeaderLength + payloadLength];
			Buffer.BlockCopy(header, 0, data, 0, HeaderLength);

			int payloadBytesRead = 0;
			while (payloadBytesRead < payloadLength)
			{
				var thisRead = await stream.ReadAsync(data, payloadBytesRead + HeaderLength, payloadLength - payloadBytesRead).ConfigureAwait(false);
				if (thisRead == 0)
					throw new SSLInvalidPacketException("Stream was closed mid-payload", data, HeaderLength + payloadBytesRead);
				payloadBytesRead += thisRead;
			}

			return new SSLPacket(data);
		}

		public Task WriteToStreamAsync(Stream stream)
		{
			return stream.WriteAsync(Data, 0, Data.Length);
		}
	}
}
