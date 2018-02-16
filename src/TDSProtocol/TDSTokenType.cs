using System;

namespace TDSProtocol
{
	public enum TDSTokenType : byte
	{
		AltMetaData = 0x88,
		AltRow = 0xD3,
		ColMetaData = 0x81,
		ColInfo = 0xA5,
		Done = 0xFD,
		DoneProc = 0xFE,
		DoneInProc = 0xFF,
		EnvChange = 0xE3,
		Error = 0xAA,
		FeatureExtAck = 0xAE, // TDS 7.4+
		Info = 0xAB,
		LoginAck = 0xAD,
		NbcRow = 0xD2, // TDS 7.3+
		Offset = 0x78,
		Order = 0xA9,
		ReturnStatus = 0x79,
		ReturnValue = 0xAC,
		Row = 0xD1,
		SessionState = 0xE4, // TDS 7.4+
		Sspi = 0xED,
		TabName = 0xA4
	}
}
