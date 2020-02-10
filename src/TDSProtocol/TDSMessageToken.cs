using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public abstract class TDSMessageToken : TDSToken
	{
		protected TDSMessageToken(TDSTokenStreamMessage owningMessage) : base(owningMessage)
		{
		}

		private const int FixedLength =
			4 + // Number
			1 + // State
			1 + // Class
			2 + // MsgText length
			1 + // ServerName length
			1;  // ProcName length

		#region Number

		private int _number;

		public int Number
		{
			get => _number;
			set
			{
				Message.Payload = null;
				_number = value;
			}
		}

		#endregion

		#region State

		private byte _state;

		public byte State
		{
			get => _state;
			set
			{
				Message.Payload = null;
				_state = value;
			}
		}

		#endregion

		#region Class

		private byte _class;

		public byte Class
		{
			get => _class;
			set
			{
				Message.Payload = null;
				_class = value;
			}
		}

		#endregion

		#region MsgText

		private string _msgText;

		public string MsgText
		{
			get => _msgText;
			set
			{
				Message.Payload = null;
				_msgText = value;
			}
		}

		#endregion

		#region ServerName

		private string _serverName;

		public string ServerName
		{
			get => _serverName;
			set
			{
				Message.Payload = null;
				_serverName = value;
			}
		}

		#endregion

		#region ProcName

		private string _procName;

		public string ProcName
		{
			get => _procName;
			set
			{
				Message.Payload = null;
				_procName = value;
			}
		}

		#endregion

		#region LineNumber

		private int _lineNumber;

		public int LineNumber
		{
			get => _lineNumber;
			set
			{
				Message.Payload = null;
				_lineNumber = value;
			}
		}

		#endregion

		#region WriteBodyToBinaryWriter

		protected override void WriteBodyToBinaryWriter(BinaryWriter bw)
		{
			var length = (ushort)(
				                     FixedLength +
				                     (MsgText?.Length * 2 ?? 0) +
				                     (ServerName?.Length * 2 ?? 0) +
				                     (ProcName?.Length * 2 ?? 0) +
				                     (TdsVersion >= 0x7200000 ? 4 : 2) // LineNumber
			                     );
			bw.Write(length);
			bw.Write(Number);
			bw.Write(State);
			bw.Write(Class);
			bw.WriteUsVarchar(MsgText);
			bw.WriteBVarchar(ServerName);
			bw.WriteBVarchar(ProcName);
			if (TdsVersion >= 0x72000000)
				bw.Write(LineNumber);
			else
				bw.Write((ushort)LineNumber);
		}

		#endregion

		#region ReadFromBinaryReader

		protected override int ReadFromBinaryReader(BinaryReader br)
		{
			if (null == br) throw new ArgumentNullException(nameof(br));

			try
			{
				var vsFixedLength = FixedLength + (TdsVersion >= 0x7200000 ? 4 : 2);

				var length = br.ReadUInt16();

				if (length < vsFixedLength)
					throw new TDSInvalidMessageException(
						$"{TokenId} token too short (min length {FixedLength}, actual length {length})",
						Message.MessageType,
						Message.Payload);

				_number = br.ReadInt32();
				_state = br.ReadByte();
				_class = br.ReadByte();

				var textLen = br.ReadUInt16();
				_msgText = br.ReadUnicode(textLen);

				textLen = br.ReadByte();
				_serverName = br.ReadUnicode(textLen);

				textLen = br.ReadByte();
				_procName = br.ReadUnicode(textLen);

				_lineNumber = TdsVersion >= 0x72000000 ? br.ReadInt32() : br.ReadUInt16();

				var lenRead = vsFixedLength + 2 * (_msgText.Length + _serverName.Length + _procName.Length);
				if (length > lenRead) br.BaseStream.Seek(length - lenRead, SeekOrigin.Current);

				return length + 3;
			}
			catch (EndOfStreamException)
			{ 
				throw new TDSInvalidMessageException("Attempted to read longer than remainder of payload",
			                                     Message.MessageType,
			                                     Message.Payload);
			}
		}

		#endregion

		#region Override ToString

		public override string ToString()
		{
			return string.IsNullOrEmpty(ProcName)
				       ? $"Message {Number}, State {State}, Class {Class}, Line {LineNumber}: {MsgText}"
				       : $"Message {Number}, State {State}, Class {Class}, Procedure {ProcName}, Line {LineNumber}: {MsgText}";
		}

		#endregion
	}
}
