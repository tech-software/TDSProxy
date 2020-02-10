using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public abstract class TDSTokenStreamMessage : TDSMessage
	{
		#region Log4Net

		static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		#endregion

		#region RawMessage

		public byte[] RawMessage
		{
			get => Payload;
			set => Payload = value;
		}

		#endregion

		#region Tokens

		private readonly List<TDSToken> _tokens = new List<TDSToken>();

		public IEnumerable<TDSToken> Tokens => _tokens;

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

		public void AddTokens(params TDSToken[] tokens) => AddTokens((IEnumerable<TDSToken>)tokens);

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
			if (null == Payload)
				throw new InvalidOperationException("Attempted to interpret payload, but no payload to interpret");

			using (var ms = new MemoryStream(Payload))
			using (var br = new BinaryReader(ms))
			{
				bool isDone = false;
				int offs = 0;
				int tn = 0;
				while (!isDone)
				{
					tn++;
					TDSToken token;
					try
					{
						token = TDSToken.ReadFromBinaryReader(this, br, offs);
					}
					catch (TDSInvalidMessageException ime)
					{
						throw new TDSInvalidMessageException($"Error parsing token #{tn} starting at offset {offs}",
						                                     MessageType,
						                                     ReceivedPayload,
						                                     ime);
					}
					catch (Exception e)
					{
						throw new Exception($"Error parsing token #{tn} starting at offset {offs}", e);
					}

					log.DebugFormat("Read token #{0} at offset {1} of type {2}, length {3}", tn, offs, token.TokenId, token.ReceivedLength);

					AddToken(token);
					isDone = token is TDSDoneToken done && !done.Status.HasFlag(TDSDoneToken.StatusEnum.More);
					offs += token.ReceivedLength;
				}
			}
		}

		#endregion
	}
}
