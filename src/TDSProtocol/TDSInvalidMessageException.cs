using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public class TDSInvalidMessageException : Exception
	{
		private readonly TDSMessageType _type;
		private readonly byte[] _payload;

		public TDSMessageType MessageType
		{
			get { return _type; }
		}

		public byte[] Payload
		{
			get { return _payload; }
		}

		public TDSInvalidMessageException(string message, TDSMessageType _type, byte[] payload) : base(message)
		{
			_payload = new byte[payload.Length];
			Buffer.BlockCopy(payload, 0, _payload, 0, payload.Length);
		}

		public string PayloadFormatted
		{
			get
			{
				return _payload.FormatAsHex();
			}
		}
	}
}
