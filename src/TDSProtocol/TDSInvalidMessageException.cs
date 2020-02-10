using System;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public class TDSInvalidMessageException : Exception
	{
		public TDSMessageType MessageType { get; }

		public byte[] Payload { get; }

		public TDSInvalidMessageException(string message,
		                                  TDSMessageType type,
		                                  byte[] payload,
		                                  Exception innerException = null) : base(message, innerException)
		{
			MessageType = type;
			Payload = new byte[payload.Length];
			Buffer.BlockCopy(payload, 0, Payload, 0, payload.Length);
		}


		public string PayloadFormatted => Payload.FormatAsHex();
	}
}
