using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public abstract class TDSMessage
	{
		public abstract TDSMessageType MessageType { get; }
		protected internal byte[] Payload { get; set; }
		protected internal byte[] ReceivedPayload { get; private set; }

		public static TDSMessage FromPackets(IEnumerable<TDSPacket> packets, TDSMessageType? overrideMessageType = null)
		{
			if (null == packets)
				return null;

			var packetList = packets as List<TDSPacket> ?? packets.ToList();
			if (packetList.Count == 0)
				return null;

			var firstPacket = packetList[0];
			if (!ConcreteTypeConstructors.TryGetValue(overrideMessageType ?? firstPacket.PacketType, out var constructor))
			{
				var packetData = firstPacket.PacketData;
				throw new TDSInvalidPacketException("Unrecognized TDS message type 0x" + ((byte)firstPacket.PacketType).ToString("X2"), packetData, packetData.Length);
			}

			// Instantiate the concrete message type and fill out the payload
			TDSMessage message  = constructor();
			byte[] payload = new byte[packetList.Sum(p => p.Payload.Length)];
			int payloadOffset = 0;
			foreach (var packet in packetList)
			{
				Buffer.BlockCopy(packet.Payload, 0, payload, payloadOffset, packet.Payload.Length);
				payloadOffset += packet.Payload.Length;
			}
			message.Payload = payload;
			message.ReceivedPayload = payload;

			message.InterpretPayload();

			return message;
		}

		public virtual string DumpReceivedPayload(string prefix = null) => ReceivedPayload.FormatAsHex(prefix);
		public virtual string DumpPayload(string prefix = null) => Payload.FormatAsHex(prefix);

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
				         Type.EmptyTypes,
				         null) ??
			         throw new InvalidOperationException($"Unable to find default constructor for type {t.FullName}");

			// Generate IL function to invoke the default constructor
			var dm = new DynamicMethod("_dynamic_constructor", t, Type.EmptyTypes, t);
			var ilg = dm.GetILGenerator();
			ilg.Emit(OpCodes.Newobj, ci);
			ilg.Emit(OpCodes.Ret);
			return (Func<TDSMessage>)dm.CreateDelegate(typeof(Func<TDSMessage>));
		}

		private static readonly Dictionary<TDSMessageType, Func<TDSMessage>> ConcreteTypeConstructors =
			(
				from cls in Assembly.GetExecutingAssembly().GetTypes()
				where !cls.IsAbstract && typeof(TDSMessage).IsAssignableFrom(cls)
				select cls
			).ToDictionary(t => MakeConstructor(t)().MessageType, MakeConstructor);

		#endregion
	}
}
