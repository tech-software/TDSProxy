using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public class TDSErrorToken : TDSToken
	{
		public TDSErrorToken(TDSTokenStreamMessage owningMessage) : base(owningMessage) { }

		public override TDSTokenType TokenId
		{
			get { return TDSTokenType.Error; }
		}

		#region Number
		private int _number;
		public int Number
		{
			get { return _number; }
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
			get { return _state; }
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
			get { return _class; }
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
			get { return _msgText; }
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
			get { return _serverName; }
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
			get { return _procName; }
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
			get { return _lineNumber; }
			set
			{
				Message.Payload = null;
				_lineNumber = value;
			}
		}
		#endregion

		#region WriteBodyToBinaryWriter
		protected override void WriteBodyToBinaryWriter(System.IO.BinaryWriter bw)
		{
			var length = (ushort)(
				1 + // TokenType
				2 + // Length
				4 + // Number
				1 + // State
				1 + // Class
				2 + // MsgText length
				1 + // ServerName length
				1 + // ProcName length
				(null == MsgText ? 0 : MsgText.Length) +
				(null == ServerName ? 0 : ServerName.Length) +
				(null == ProcName ? 0 : ProcName.Length) +
				(TDSToken.TdsVersion >= 0x7200000 ? 4 : 2) // LineNumber
				);
			bw.Write(length);
			bw.Write(Number);
			bw.Write(State);
			bw.Write(Class);
			bw.Write((ushort)(null == MsgText ? 0 : MsgText.Length));
			bw.Write((ushort)(null == ServerName ? 0 : ServerName.Length));
			bw.Write((ushort)(null == ProcName ? 0 : ProcName.Length));
			if (TDSToken.TdsVersion >= 0x72000000)
				bw.Write(LineNumber);
			else
				bw.Write((ushort)LineNumber);
		}
		#endregion
	}
}
