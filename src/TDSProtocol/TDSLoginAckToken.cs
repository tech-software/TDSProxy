using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public class TDSLoginAckToken : TDSToken
	{
		protected TDSLoginAckToken(TDSTokenStreamMessage owningMessage) : base(owningMessage)
		{
		}

		public override TDSTokenType TokenId => TDSTokenType.LoginAck;

		private const ushort FixedLength = 10;

		#region Length

		public ushort Length => (ushort)(FixedLength + (ProgName?.Length).GetValueOrDefault());

		#endregion

		#region Interface

		private byte _interface;

		public byte Interface
		{
			get => _interface;
			set
			{
				Message.Payload = null;
				_interface = value;
			}
		}

		#endregion

		#region TDSVersion

		private uint _tdsVersion;

		public uint TDSVersion
		{
			get => _tdsVersion;
			set
			{
				Message.Payload = null;
				_tdsVersion = value;
			}
		}

		#endregion

		#region ProgName

		// ReSharper disable IdentifierTypo

		private string _progName;

		public string ProgName
		{
			get => _progName;
			set
			{
				Message.Payload = null;
				_progName = value;
			}
		}

		// ReSharper restore IdentifierTypo

		#endregion

		#region ProgVersion

		// ReSharper disable IdentifierTypo

		public struct ProgVersionStruct
		{
			public byte MajorVer { get; set; }
			public byte MinorVer { get; set; }
			public byte BuildNumHi { get; set; }
			public byte BuildNumLo { get; set; }
		}

		private ProgVersionStruct _progVersion;

		public ProgVersionStruct ProgVersion
		{
			get => _progVersion;
			set
			{
				Message.Payload = null;
				_progVersion = value;
			}
		}

		// ReSharper restore IdentifierTypo

		#endregion

		#region WriteBodyToBinaryWriter

		protected override void WriteBodyToBinaryWriter(BinaryWriter bw)
		{
			bw.Write(Length);
			bw.Write(Interface);
			bw.Write(TDSVersion);
			bw.WriteBVarchar(ProgName);
			bw.Write(ProgVersion.MajorVer);
			bw.Write(ProgVersion.MinorVer);
			bw.Write(ProgVersion.BuildNumHi);
			bw.Write(ProgVersion.BuildNumLo);
		}

		#endregion

		#region ReadBodyFromBinaryReader

		protected override int ReadFromBinaryReader(BinaryReader br)
		{
			if (null == br) throw new ArgumentNullException(nameof(br));

			try
			{
				var length = br.ReadUInt16();
				if (length < FixedLength)
					throw new TDSInvalidMessageException(
						$"LoginAck token too short (min length {FixedLength}, actual length {length})",
						Message.MessageType,
						Message.Payload);

				_interface = br.ReadByte();
				_tdsVersion = br.ReadUInt32();
				var pnLength = br.ReadByte();
				if (pnLength + FixedLength > length)
					throw new TDSInvalidMessageException(
						// ReSharper disable StringLiteralTypo
						$"ProgName exceeds LoginAck token size (ProgName length {pnLength}, fixed part {FixedLength}, token length {length}",
						// ReSharper restore StringLiteralTypo
						Message.MessageType,
						Message.Payload);
				_progName = br.ReadUnicode(pnLength);
				_progVersion = new ProgVersionStruct
				               {
					               MajorVer = br.ReadByte(),
					               MinorVer = br.ReadByte(),
					               BuildNumHi = br.ReadByte(),
					               BuildNumLo = br.ReadByte()
				               };

				// Note: big-L length is calculated from the fields we've read
				if (Length < length) br.BaseStream.Seek(length - Length, SeekOrigin.Current);

				return length + 2;
			}
			catch (EndOfStreamException)
			{
				throw new TDSInvalidMessageException("Attempted to read longer than remainder of payload",
				                                     Message.MessageType,
				                                     Message.Payload);
			}
		}

		#endregion
	}
}
