using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public class SMPPacket
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		protected const int HeaderLength = 16;

		private static readonly HashSet<SmpPacketType> _knownPacketTypes = new HashSet<SmpPacketType>(Enum.GetValues(typeof(SmpPacketType)).Cast<SmpPacketType>());
		public static bool IsSMPPacketType(byte packetTypeByte)
		{
			return _knownPacketTypes.Contains((SmpPacketType)packetTypeByte);
		}

		internal readonly byte[] _data;

		protected SMPPacket(byte[] data)
		{
			if (null == data)
				throw new ArgumentNullException("data");
			if (data.Length < HeaderLength)
				throw new ArgumentOutOfRangeException("data", "Data length insufficient to contain SMP header.");
			if (!IsSMPPacketType(data[0]))
				throw new ArgumentOutOfRangeException("data", "Not an SMP packet type: " + data[0].ToString("X2"));
			if (data.Length != GetPacketLength(data))
				throw new ArgumentOutOfRangeException("data", "Data length does not match length in header.");
			_data = data;
		}

		private static uint GetPacketLength(byte[] data)
		{
			return unchecked((uint)((data[7] << 24) | (data[6] << 16) | (data[5] << 8) | (data[4])));
		}

		public SmpPacketType PacketType
		{
			get { return (SmpPacketType)_data[0]; }
		}

		public SmpFlags Flags
		{
			get { return (SmpFlags)_data[1]; }
		}

		public ushort SID
		{
			get { return unchecked((ushort)((_data[3] << 8) | _data[2])); }
		}

		public uint Length
		{
			get { return GetPacketLength(_data); }
		}

		public uint SeqNum
		{
			get
			{
				return unchecked((uint)((_data[11] << 24) | (_data[10] << 16) | (_data[9] << 8) | _data[8]));
			}
		}

		public uint Window
		{
			get
			{
				return unchecked((uint)((_data[15] << 24) | (_data[14] << 16) | (_data[13] << 8) | _data[12]));
			}
		}

		public byte[] Payload
		{
			get
			{
				var pl = new byte[Length - HeaderLength];
				Buffer.BlockCopy(_data, HeaderLength, pl, 0, pl.Length);
				return pl;
			}
		}

		public static SMPPacket ReadFromStream(Stream stream, bool readPacketType, SmpPacketType? packetType)
		{
			return ReadFromStreamAsync(stream, readPacketType, packetType).Result;
		}

		public static async Task<SMPPacket> ReadFromStreamAsync(Stream stream, bool readPacketType, SmpPacketType? packetType)
		{
			if ((!readPacketType) && (!packetType.HasValue))
				throw new ArgumentException("packetType must be specified if readPacketType is false.");

			var header = new byte[HeaderLength];
			int packetBytesRead = 0;

			if (readPacketType)
			{
				packetBytesRead = await stream.ReadAsync(header, 0, 1).ConfigureAwait(false);
				if (packetBytesRead == 0)
					return null;

				if (!IsSMPPacketType(header[0]))
					throw new SMPInvalidPacketException("Packet type " + header[0].ToString("X2") + " is not a recognized MC-SMP packet type.", header, 1);
			}
			else
			{
				header[0] = (byte)packetType.Value;
				packetBytesRead = 1;
			}
			if (packetType.HasValue && (byte)packetType.Value != header[0])
				throw new SMPInvalidPacketException("Packet type " + (SmpPacketType)header[0] + " does not match specified value of " + packetType.Value + ".", header, 1);

			while (packetBytesRead < HeaderLength)
			{
				var thisRead = await stream.ReadAsync(header, packetBytesRead, HeaderLength - packetBytesRead).ConfigureAwait(false);
				if (thisRead == 0)
					throw new SSLInvalidPacketException("Stream was closed mid-header", header, packetBytesRead);
				packetBytesRead += thisRead;
			}

			var packetLength = (int)GetPacketLength(header);
			var data = new byte[packetLength];
			Buffer.BlockCopy(header, 0, data, 0, HeaderLength);

			while (packetBytesRead < packetLength)
			{
				var thisRead = await stream.ReadAsync(data, packetBytesRead, packetLength - packetBytesRead).ConfigureAwait(false);
				if (thisRead == 0)
					throw new SSLInvalidPacketException("Stream was closed mid-payload", data, HeaderLength + packetBytesRead);
				packetBytesRead += thisRead;
			}

			return new SMPPacket(data);
		}

		public void WriteToStream(Stream stream)
		{
			WriteToStreamAsync(stream).Wait();
		}

		public Task WriteToStreamAsync(Stream stream)
		{
			return stream.WriteAsync(_data, 0, _data.Length);
		}
	}
}
