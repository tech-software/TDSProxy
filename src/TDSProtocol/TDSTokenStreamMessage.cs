using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public abstract class TDSTokenStreamMessage : TDSMessage
	{
		#region RawMessage
		public byte[] RawMessage
		{
			get { return Payload; }
			set { Payload = value; }
		}
		#endregion

		#region Tokens
		private readonly List<TDSToken> _tokens = new List<TDSToken>();
		public IEnumerable<TDSToken> Tokens
		{
			get { return _tokens; }
		}
		public void ClearTokens()
		{
			_tokens.Clear();
		}
		public void AddToken(TDSToken token)
		{
			if (token.Message != this)
				throw new InvalidOperationException("Token is not associated with this message");
			_tokens.Add(token);
		}
		public void AddTokens(IEnumerable<TDSToken> tokens)
		{
			if (null != tokens)
			{
				foreach (var token in tokens)
					AddToken(token);
			}
		}
		public TDSToken FindToken(TDSTokenType tokenId)
		{
			return _tokens.FirstOrDefault(t => t.TokenId == tokenId);
		}
		#endregion

		#region BuildMessage
		public void BuildMessage()
		{
			EnsurePayload();
		}
		#endregion

		#region GeneratePayload
		protected internal override void GeneratePayload()
		{
			using (var ms = new MemoryStream())
			{
				using (var bw = new BinaryWriter(ms, Unicode.Instance, true))
				{
					foreach (var t in Tokens)
						t.WriteToBinaryWriter(bw);
				}

				Payload = ms.ToArray();
			}
		}
		#endregion

		#region InterpretPayload
		protected internal override void InterpretPayload()
		{
			throw new NotSupportedException("Did not expect to ever read a token stream message");
		}
		#endregion
	}
}
