using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public class TDSPacket
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		private static readonly HashSet<TDSMessageType> KnownPacketTypes =
			new HashSet<TDSMessageType>(Enum.GetValues(typeof(TDSMessageType)).Cast<TDSMessageType>());
		public static bool IsTDSPacketType(byte packetTypeByte)
		{
			return KnownPacketTypes.Contains((TDSMessageType)packetTypeByte);
		}

		public static bool DumpPackets { get; set; }

		public TDSMessageType PacketType { get; set; }

		public TDSStatus Status { get; set; }
		public ushort Length { get; private set; }

		public ushort SPID { get; set; }
		public byte PacketID { get; set; }
		public byte Window { get; set; }

		public byte[] Payload { get; set; }

		public byte[] PacketData
		{
			get
			{
				byte[] packetData = new byte[Length];
				packetData[0] = (byte)PacketType;
				packetData[1] = (byte)Status;
				packetData[2] = (byte)((Length >> 8) & 0xff);
				packetData[3] = (byte)(Length & 0xff);
				packetData[4] = (byte)((SPID >> 8) & 0xff);
				packetData[5] = (byte)(SPID & 0xff);
				packetData[6] = PacketID;
				packetData[7] = Window;
				Buffer.BlockCopy(Payload, 0, packetData, HeaderLength, Payload.Length);

				return packetData;
			}
		}

		protected const int HeaderLength = 8;

		public static void WriteMessage(
			Stream stream, ushort packetLength, ushort spid, TDSMessage message, TDSStatus status = TDSStatus.Normal, TDSMessageType? overrideMessageType = null)
		{
			WriteMessageAsync(stream, packetLength, spid, message, status, overrideMessageType).Wait();
		}

		public static Task WriteMessageAsync(
			Stream stream, ushort packetLength, ushort spid, TDSMessage message, TDSStatus status = TDSStatus.Normal, TDSMessageType? overrideMessageType = null)
		{
			return WriteMessageAsync(stream, packetLength, spid, message, CancellationToken.None, status, overrideMessageType);
		}

		public static async Task WriteMessageAsync(
			Stream stream,
			ushort packetLength,
			ushort spid,
			TDSMessage message,
			CancellationToken cancellationToken,
			TDSStatus status = TDSStatus.Normal,
			TDSMessageType? overrideMessageType = null)
		{
			status &= ~TDSStatus.EndOfMessage;

			message.EnsurePayload();
			var payload = message.Payload;
			var packet = new byte[Math.Min(payload.Length + HeaderLength, packetLength)];
			int payloadOffset = 0;
			int maxPacketPayload = packetLength - HeaderLength;
			var messageType = overrideMessageType ?? message.MessageType;

			byte packetId = 1;
			while (payloadOffset < payload.Length)
			{
				ushort thisPayloadLength = (ushort)Math.Min(payload.Length - payloadOffset, maxPacketPayload);
				ushort thisPacketLength = (ushort)(thisPayloadLength + HeaderLength);
				if (payloadOffset + thisPayloadLength >= payload.Length)
					status |= TDSStatus.EndOfMessage;

				packet[0] = (byte)messageType;
				packet[1] = (byte)status;
				packet[2] = (byte)((thisPacketLength >> 8) & 0xff);
				packet[3] = (byte)(thisPacketLength & 0xff);
				packet[4] = (byte)((spid >> 8) & 0xff);
				packet[5] = (byte)(spid & 0xff);
				packet[6] = packetId++;
				packet[7] = 0;
				Buffer.BlockCopy(payload, payloadOffset, packet, HeaderLength, thisPayloadLength);
				await stream.WriteAsync(packet, 0, thisPacketLength, cancellationToken);

				if (DumpPackets)
					log.DebugFormat("Wrote {0} packet. Data:\r\n{1}", messageType, packet.FormatAsHex());

				payloadOffset += thisPayloadLength;
			}
		}

		public Task WriteToStreamAsync(Stream stream)
		{
			var packetData = PacketData;
			return stream.WriteAsync(packetData, 0, packetData.Length);
		}

		public static Task<IEnumerable<TDSPacket>> ReadAsync(Stream stream)
		{
			return ReadAsync(stream, CancellationToken.None);
		}

		public static async Task<IEnumerable<TDSPacket>> ReadAsync(Stream stream, CancellationToken cancellationToken)
		{
			byte[] peekBuffer = new byte[1];
			int typeByteRead = await stream.ReadAsync(peekBuffer, 0, 1, cancellationToken).ConfigureAwait(false);
			if (typeByteRead <= 0)
				throw new EndOfStreamException();

			return await ReadAsync((TDSMessageType)peekBuffer[0], stream, cancellationToken).ConfigureAwait(false);
		}

		public static IEnumerable<TDSPacket> Read(Stream stream)
		{
			int typeByte = stream.ReadByte();
			if (typeByte < 0)
				throw new EndOfStreamException();

			return Read((TDSMessageType)typeByte, stream);
		}

		public static Task<IEnumerable<TDSPacket>> ReadAsync(TDSMessageType type, Stream stream)
		{
			return ReadAsync(type, stream, CancellationToken.None);
		}

		public static async Task<IEnumerable<TDSPacket>> ReadAsync(TDSMessageType type, Stream stream, CancellationToken cancellationToken)
		{
			List<TDSPacket> packets = new List<TDSPacket>();
			TDSPacket lastPacket;
			bool notFirstPacket = false;
			do
			{
				lastPacket = await ReadSinglePacketAsync(type, stream, notFirstPacket, cancellationToken).ConfigureAwait(false);
				packets.Add(lastPacket);
				notFirstPacket = true;
			}
			while ((lastPacket.Status & TDSStatus.EndOfMessage) != TDSStatus.EndOfMessage);

			return packets;
		}

		public static IEnumerable<TDSPacket> Read(TDSMessageType type, Stream stream)
		{
			return ReadAsync(type, stream).Result;
		}

		public static Task<TDSPacket> ReadSinglePacketAsync(TDSMessageType type, Stream stream, bool readPacketTypeFromStream)
		{
			return ReadSinglePacketAsync(type, stream, readPacketTypeFromStream, CancellationToken.None);
		}

		public static async Task<TDSPacket> ReadSinglePacketAsync(
			TDSMessageType type, Stream stream, bool readPacketTypeFromStream, CancellationToken cancellationToken)
		{
			// Already have the type, read the rest of the TDS packet header
			var header = new byte[HeaderLength];
			int packetBytesRead = 0;
			if (!readPacketTypeFromStream)
			{
				header[0] = (byte)type;
				packetBytesRead = 1;
			}
			while (packetBytesRead < header.Length)
			{
				var thisRead = await stream.ReadAsync(header, packetBytesRead, header.Length - packetBytesRead, cancellationToken).ConfigureAwait(false);
				if (thisRead <= 0)
					throw new TDSInvalidPacketException("Incomplete TDS packet header received.", header, packetBytesRead);
				packetBytesRead += thisRead;
			}

			// Break out fields from the packet header
			var status = (TDSStatus)header[1];
			var length = (ushort)((header[2] << 8) + header[3]);
			if (length < header.Length)
				throw new TDSInvalidPacketException("Invalid length (" + length + ") in TDS packet.", header, packetBytesRead);
			var spid = (ushort)((header[4] << 8) + header[5]);
			var packetId = header[6];
			var window = header[7];

			var payload = new byte[length - HeaderLength];

			// Read rest of packet
			while (packetBytesRead < length)
			{
				var thisRead = await stream.ReadAsync(payload, packetBytesRead - HeaderLength, length - packetBytesRead, cancellationToken).ConfigureAwait(false);
				if (thisRead <= 0)
				{
					var packetData = new byte[packetBytesRead];
					Buffer.BlockCopy(header, 0, packetData, 0, HeaderLength);
					Buffer.BlockCopy(payload, 0, packetData, HeaderLength, packetBytesRead - HeaderLength);
					throw new TDSInvalidPacketException(
						"Connection closed before complete TDS packet could be read, got " + packetBytesRead + " bytes, expected " + length + " bytes.",
						packetData,
						packetBytesRead);
				}
				packetBytesRead += thisRead;
			}

			if ((TDSMessageType)header[0] != type)
			{
				var packetData = new byte[packetBytesRead];
				Buffer.BlockCopy(header, 0, packetData, 0, HeaderLength);
				Buffer.BlockCopy(payload, 0, packetData, HeaderLength, packetBytesRead - HeaderLength);
				throw new TDSInvalidPacketException(
					$"Unexpected message type, expected {type} got {(TDSMessageType)header[0]}", null, 0);
			}

			if (DumpPackets)
				log.DebugFormat("Received {0} packet. Data:\r\n{1}", type, header.Concat(payload).FormatAsHex());

			return new TDSPacket
			{
				PacketType = type,
				Status = status,
				Length = length,
				SPID = spid,
				PacketID = packetId,
				Window = window,
				Payload = payload
			};
		}

		public static TDSPacket ReadSinglePacket(TDSMessageType type, Stream stream, bool readPacketTypeFromStream)
		{
			return ReadSinglePacketAsync(type, stream, readPacketTypeFromStream).Result;
		}
	}
}
