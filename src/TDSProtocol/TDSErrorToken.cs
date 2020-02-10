using System;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public class TDSErrorToken : TDSMessageToken
	{
		public TDSErrorToken(TDSTokenStreamMessage owningMessage) : base(owningMessage) { }

		public override TDSTokenType TokenId => TDSTokenType.Error;
	}
}
