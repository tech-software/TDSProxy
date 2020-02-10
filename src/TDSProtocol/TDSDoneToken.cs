using System;
using System.IO;
using JetBrains.Annotations;

namespace TDSProtocol
{
	public class TDSDoneToken : TDSToken
	{
		public TDSDoneToken(TDSTokenStreamMessage owningMessage) : base(owningMessage)
		{
		}

		public override TDSTokenType TokenId => TDSTokenType.Done;

		private const int LengthBeforeTDS72 = 8;
		private const int LengthTDS72AndAfter = 12;

		public int Length => TdsVersion >= 0x72000000 ? LengthTDS72AndAfter : LengthBeforeTDS72;

		#region Status

		[PublicAPI, Flags]
		public enum StatusEnum : ushort
		{
			Final = 0x0000,
			More = 0x0001,
			Error = 0x0002,

			// ReSharper disable once IdentifierTypo
			InXact = 0x0004,
			Count = 0x0010,
			Attn = 0x0020,
			SrvError = 0x0100
		}

		private StatusEnum _status;

		public StatusEnum Status
		{
			get => _status;
			set
			{
				Message.Payload = null;
				_status = value;
			}
		}

		#endregion

		#region CurCmd

		private ushort _curCmd;

		public ushort CurCmd
		{
			get => _curCmd;
			set
			{
				Message.Payload = null;
				_curCmd = value;
			}
		}

		#endregion

		#region DoneRowCount

		private ulong _doneRowCount;

		public ulong DoneRowCount
		{
			get => _doneRowCount;
			set
			{
				Message.Payload = null;
				_doneRowCount = value;
			}
		}

		#endregion

		#region WriteToBinaryWriter

		protected override void WriteBodyToBinaryWriter(BinaryWriter bw)
		{
			bw.Write((ushort)Status);
			bw.Write(CurCmd);
			if (TdsVersion >= 0x72000000)
				bw.Write(DoneRowCount);
			else
				bw.Write((uint)DoneRowCount);
		}

		#endregion

		#region ReadFromBinaryReader

		protected override int ReadFromBinaryReader(BinaryReader br)
		{
			try
			{
				_status = (StatusEnum)br.ReadUInt16();
				_curCmd = br.ReadUInt16();
				_doneRowCount = TdsVersion >= 0x72000000 ? br.ReadUInt64() : br.ReadUInt32();
				return Length;
			}
			catch (EndOfStreamException)
			{
				throw new TDSInvalidMessageException("Attempted to read longer than payload",
				                                     Message.MessageType,
				                                     Message.Payload);
			}
		}

		#endregion
	}
}
