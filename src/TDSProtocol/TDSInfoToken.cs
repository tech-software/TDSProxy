using System;

namespace TDSProtocol
{
	public class TDSInfoToken : TDSMessageToken
	{
		public TDSInfoToken(TDSTokenStreamMessage message) : base(message) { }

		public override TDSTokenType TokenId => TDSTokenType.Info;
	}
}
