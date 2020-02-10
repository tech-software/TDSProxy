using System;
using System.IO;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public class TDSEnvChangeToken : TDSToken
	{
		public TDSEnvChangeToken(TDSTokenStreamMessage message) : base(message)
		{
		}

		public override TDSTokenType TokenId => TDSTokenType.EnvChange;

		#region Length

		private ushort? _length;

		public ushort Length => _length ?? (ushort)(1 + _newValue.Length + _oldValue.Length);

		#endregion

		#region Type

		[PublicAPI]
		public enum EnvChangeType : byte
		{
			Database = 1,
			Language = 2,
			CharacterSet = 3,
			PacketSize = 4,
			UnicodeSortingLocalId = 5,
			UnicodeSortingComparisonFlags = 6,
			SqlCollation = 7,
			BeginTransaction = 8,
			CommitTransaction = 9,
			RollbackTransaction = 10,
			EnlistDtcTransaction = 11,
			DefectTransaction = 12,
			DatabaseMirroringPartner = 13,
			PromoteTransaction = 15,
			TransactionManagerAddress = 16,
			TransactionEnded = 17,
			ResetConnectionCompletionAck = 18,
			NameOfUserInstanceStarted = 19,
			RoutingInformation = 20,
		}

		private EnvChangeType _type;

		public EnvChangeType Type
		{
			get => _type;
			set
			{
				Message.Payload = null;
				_length = null;
				_type = value;

				switch (value)
				{
				case EnvChangeType.UnicodeSortingLocalId:
				case EnvChangeType.UnicodeSortingComparisonFlags:
				case EnvChangeType.BeginTransaction:
				case EnvChangeType.DefectTransaction:
				case EnvChangeType.DatabaseMirroringPartner:
				case EnvChangeType.PromoteTransaction:
				case EnvChangeType.TransactionManagerAddress:
				case EnvChangeType.NameOfUserInstanceStarted:
					_oldValue = new byte[] {0};
					break;

				case EnvChangeType.RoutingInformation:
					_oldValue = new byte[] {0, 0};
					break;

				case EnvChangeType.CommitTransaction:
				case EnvChangeType.RollbackTransaction:
				case EnvChangeType.EnlistDtcTransaction:
				case EnvChangeType.TransactionEnded:
					_newValue = new byte[] {0};
					break;

				case EnvChangeType.ResetConnectionCompletionAck:
					_oldValue = new byte[] {0};
					_newValue = new byte[] {0};
					break;

				}
			}
		}

		#endregion

		#region OldValue

		private byte[] _oldValue = {0};

		public byte[] OldValue
		{
			get => _oldValue;
			set
			{
				if (value is null) throw new ArgumentNullException(nameof(OldValue));

				switch (_type)
				{
				case EnvChangeType.UnicodeSortingLocalId:
				case EnvChangeType.UnicodeSortingComparisonFlags:
				case EnvChangeType.BeginTransaction:
				case EnvChangeType.DefectTransaction:
				case EnvChangeType.DatabaseMirroringPartner:
				case EnvChangeType.PromoteTransaction:
				case EnvChangeType.TransactionManagerAddress:
				case EnvChangeType.ResetConnectionCompletionAck:
				case EnvChangeType.NameOfUserInstanceStarted:
				case EnvChangeType.RoutingInformation:
					throw new InvalidOperationException($"EnvChange of type {_type} has null/fixed OldValue");
				}

				Message.Payload = null;
				_length = null;
				_oldValue = value;
			}
		}

		#endregion

		#region NewValue

		private byte[] _newValue = {0};

		public byte[] NewValue
		{
			get => _newValue;
			set
			{
				if (value is null) throw new ArgumentNullException(nameof(NewValue));

				switch (_type)
				{
				case EnvChangeType.CommitTransaction:
				case EnvChangeType.RollbackTransaction:
				case EnvChangeType.EnlistDtcTransaction:
				case EnvChangeType.TransactionEnded:
				case EnvChangeType.ResetConnectionCompletionAck:
					throw new InvalidOperationException($"EnvChange of type {_type} has null/fixed NewValue");
				}

				Message.Payload = null;
				_length = null;
				_newValue = value;
			}
		}

		#endregion

		#region WriteBodyToBinaryWriter

		protected override void WriteBodyToBinaryWriter(BinaryWriter bw)
		{
			bw.Write(Length);
			bw.Write((byte)Type);
			bw.Write(_oldValue);
			bw.Write(_newValue);
		}

		#endregion

		#region ReadFromBinaryReader

		protected override int ReadFromBinaryReader(BinaryReader br)
		{
			try
			{
				_length = br.ReadUInt16();
				_type = (EnvChangeType)br.ReadByte();
			}
			catch (EndOfStreamException)
			{
				throw new TDSInvalidMessageException("Attempted to read longer than payload",
				                                     Message.MessageType,
				                                     Message.Payload);
			}

			switch (_type)
			{
			case EnvChangeType.Database:
			case EnvChangeType.Language:
			case EnvChangeType.CharacterSet:
			case EnvChangeType.PacketSize:
			case EnvChangeType.UnicodeSortingLocalId:
			case EnvChangeType.UnicodeSortingComparisonFlags:
			case EnvChangeType.DatabaseMirroringPartner:
			case EnvChangeType.NameOfUserInstanceStarted:
				// B_VARCHAR
				_newValue = ReadData(br, (uint)(_length.GetValueOrDefault() - 1), true, 1, true);
				_oldValue = ReadData(br, (uint)(_length.GetValueOrDefault() - (1 + _newValue.Length)), true, 1, true);
				break;

			case EnvChangeType.SqlCollation:
			case EnvChangeType.BeginTransaction:
			case EnvChangeType.CommitTransaction:
			case EnvChangeType.RollbackTransaction:
			case EnvChangeType.EnlistDtcTransaction:
			case EnvChangeType.DefectTransaction:
			case EnvChangeType.TransactionManagerAddress:
			case EnvChangeType.TransactionEnded:
			case EnvChangeType.ResetConnectionCompletionAck:
				// B_VARBYTE
				_newValue = ReadData(br, (uint)(_length.GetValueOrDefault() - 1), true, 1, false);
				_oldValue = ReadData(br, (uint)(_length.GetValueOrDefault() - (1 + _newValue.Length)), true, 1, false);
				break;

			case EnvChangeType.RoutingInformation:
				// US_VARBYTE
				_newValue = ReadData(br, (uint)(_length.GetValueOrDefault() - 1), true, 2, false);
				_oldValue = ReadData(br, (uint)(_length.GetValueOrDefault() - (1 + _newValue.Length)), true, 2, false);
				break;

			case EnvChangeType.PromoteTransaction:
				// L_VARBYTE
				_newValue = ReadData(br, (uint)(_length.GetValueOrDefault() - 1), true, 4, false);
				_oldValue = ReadData(br, (uint)(_length.GetValueOrDefault() - (1 + _newValue.Length)), true, 4, false);
				break;

			default:
				throw new TDSInvalidMessageException($"Unknown EnvChange type {_type}",
				                                     Message.MessageType,
				                                     Message.Payload);
			}

			return _length.GetValueOrDefault() + 2;
		}

		#endregion

		#region Helper Methods

		private byte[] ReadData(BinaryReader br, uint lengthRemaining, bool isNew, int lengthOfLength, bool isVarchar)
		{
			if (lengthRemaining < lengthOfLength)
				throw ValueOverflowsToken(isNew);

			int length;
			switch (lengthOfLength)
			{
			case 1:
				length = br.ReadByte();
				break;
			case 2:
				length = br.ReadUInt16();
				break;
			case 4:
				length = br.ReadInt32();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(lengthOfLength), lengthOfLength, "Must be 1, 2, or 4.");
			}

			var byteLen = isVarchar ? length << 1 : length;

			if (byteLen + lengthOfLength > lengthRemaining)
				throw ValueOverflowsToken(isNew);

			var data = new byte[lengthOfLength + byteLen];
			data[0] = (byte)length;
			if (lengthOfLength > 1)
			{
				data[1] = (byte)(length >> 8);
				if (lengthOfLength > 2)
				{
					data[2] = (byte)(length >> 16);
					data[3] = (byte)(length >> 24);
				}
			}

			if (byteLen > 0)
				br.Read(data, lengthOfLength, byteLen);

			return data;
		}

		private TDSInvalidMessageException ValueOverflowsToken(bool isNew) =>
			new TDSInvalidMessageException(
				$"{(isNew ? "NewValue" : "OldValue")} of {_type} Environment Change Token overflows token length",
				Message.MessageType,
				Message.Payload);

		#endregion
	}
}
