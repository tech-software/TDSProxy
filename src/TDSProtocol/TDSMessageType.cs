using System;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public enum TDSMessageType : byte
	{
		SqlBatch = 1,
		PreTDS7Login = 2,
		Rpc = 3,
		TabularResult = 4,
		Attention = 6,
		BulkLoad = 7,
		FederatedAuth = 8,
		TransactionManagerRequest = 14,
		Login7 = 16,
		SspiMessage = 17,
		PreLogin = 18
	}
}
