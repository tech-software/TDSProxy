using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public abstract class TDSMessage
	{
		public abstract TDSMessageType MessageType { get; }
		protected internal byte[] Payload { get; set; }

		public static TDSMessage FromPackets(IEnumerable<TDSPacket> packets, TDSMessageType? overrideMessageType = null)
		{
			if (null == packets || !packets.Any())
				return null;

			Func<TDSMessage> constructor;
			var firstPacket = packets.First();
			if (!_concreteTypeConstructors.TryGetValue(overrideMessageType ?? firstPacket.PacketType, out constructor))
			{
				var packetData = firstPacket.PacketData;
				throw new TDSInvalidPacketException("Unrecognized TDS message type 0x" + ((byte)firstPacket.PacketType).ToString("X2"), packetData, packetData.Length);
			}

			// Instantiate the concrete message type and fill out the payload
			TDSMessage message  = constructor();
			byte[] payload = new byte[packets.Sum(p => p.Payload.Length)];
			int payloadOffset = 0;
			foreach (var packet in packets)
			{
				Buffer.BlockCopy(packet.Payload, 0, payload, payloadOffset, packet.Payload.Length);
				payloadOffset += packet.Payload.Length;
			}
			message.Payload = payload;
			message.InterpretPayload();

			return message;
		}

		public void WriteAsPackets(Stream stream, ushort packetLength, ushort spid, TDSStatus status = TDSStatus.Normal, TDSMessageType? overrideMessageType = null)
		{
			TDSPacket.WriteMessage(stream, packetLength, spid, this, status, overrideMessageType);
		}

		public Task WriteAsPacketsAsync(
			Stream stream, ushort packetLength, ushort spid, TDSStatus status = TDSStatus.Normal, TDSMessageType? overrideMessageType = null)
		{
			return WriteAsPacketsAsync(stream, packetLength, spid, CancellationToken.None, status, overrideMessageType);
		}

		public Task WriteAsPacketsAsync(
			Stream stream,
			ushort packetLength,
			ushort spid,
			CancellationToken cancellationToken,
			TDSStatus status = TDSStatus.Normal,
			TDSMessageType? overrideMessageType = null)
		{
			return TDSPacket.WriteMessageAsync(stream, packetLength, spid, this, cancellationToken, status, overrideMessageType);
		}

		internal void EnsurePayload()
		{
			if (null == Payload)
				GeneratePayload();
		}

		protected internal abstract void GeneratePayload();
		protected internal abstract void InterpretPayload();

		#region Implementation registry

		private static Func<TDSMessage> MakeConstructor(Type t)
		{
			// Get default constructor
			var ci = t.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
				null,
				System.Type.EmptyTypes,
				null);

			// Generate IL function to invoke the default constructor
			var dm = new DynamicMethod("_dynamic_constructor", t, Type.EmptyTypes, t);
			var ilg = dm.GetILGenerator();
			ilg.Emit(OpCodes.Newobj, ci);
			ilg.Emit(OpCodes.Ret);
			return (Func<TDSMessage>)dm.CreateDelegate(typeof(Func<TDSMessage>));
		}

		private readonly static Dictionary<TDSMessageType, Func<TDSMessage>> _concreteTypeConstructors =
			(
				from cls in Assembly.GetExecutingAssembly().GetTypes()
				where !cls.IsAbstract && typeof(TDSMessage).IsAssignableFrom(cls)
				select cls
			).ToDictionary(t => MakeConstructor(t)().MessageType, new Func<Type, Func<TDSMessage>>(MakeConstructor));

		#endregion
	}
}
