using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public class TDSDoneToken : TDSToken
	{
		public TDSDoneToken(TDSTokenStreamMessage owningMessage) : base(owningMessage) { }

		public override TDSTokenType TokenId
		{
			get { return TDSTokenType.Done; }
		}

		#region Status
		[Flags]
		public enum StatusEnum : ushort
		{
			Final = 0x0000,
			More = 0x0001,
			Error = 0x0002,
			InXact = 0x0004,
			Count = 0x0010,
			Attn = 0x0020,
			SrvError = 0x0100
		}
		private StatusEnum _status;
		public StatusEnum Status
		{
			get { return _status; }
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
			get { return _curCmd; }
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
			get { return _doneRowCount; }
			set
			{
				Message.Payload = null;
				_doneRowCount = value;
			}
		}
		#endregion

		#region WriteToBinaryWriter
		protected override void WriteBodyToBinaryWriter(System.IO.BinaryWriter bw)
		{
			bw.Write((ushort)Status);
			bw.Write(CurCmd);
			if (TDSToken.TdsVersion >= 0x72000000)
				bw.Write(DoneRowCount);
			else
				bw.Write((uint)DoneRowCount);
		}
		#endregion
	}
}
