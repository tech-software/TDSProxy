using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public abstract class TDSToken
	{
		#region TdsVersion

		private const string LccKeyForTdsVersion = "com.techsoftware.TDSProtocol.TdsVersion";

		/// <summary>
		/// TdsVersion for the current message processing.
		/// </summary>
		public static uint TdsVersion
		{
			get => (CallContext.LogicalGetData(LccKeyForTdsVersion) as uint?).GetValueOrDefault();
			set => CallContext.LogicalSetData(LccKeyForTdsVersion, value);
		}

		#endregion

		protected internal readonly TDSTokenStreamMessage Message;

		public int ReceivedOffset { get; private set; }

		public int ReceivedLength { get; private set; }

		protected TDSToken(TDSTokenStreamMessage message)
		{
			Message = message;
		}

		#region TokenId

		public abstract TDSTokenType TokenId { get; }

		#endregion

		#region WriteToBinaryWriter

		protected internal void WriteToBinaryWriter(BinaryWriter bw)
		{
			bw.Write((byte)TokenId);
			WriteBodyToBinaryWriter(bw);
		}

		protected abstract void WriteBodyToBinaryWriter(BinaryWriter bw);

		#endregion

		#region ReadFromBinaryReader

		protected abstract int ReadFromBinaryReader(BinaryReader br);

		protected internal static TDSToken ReadFromBinaryReader(TDSTokenStreamMessage message, BinaryReader br, int initialOffset)
		{
			var tokenId = (TDSTokenType)br.ReadByte();
			try
			{
				var token = ConcreteTypeConstructors[tokenId](message);
				token.ReceivedOffset = initialOffset;
				token.ReceivedLength = 1 + token.ReadFromBinaryReader(br);
				return token;
			}
			catch (Exception ex)
			{
				throw new TDSInvalidMessageException($"Failed to initialize {tokenId} token",
				                                     message?.MessageType ?? unchecked ((TDSMessageType)(-1)),
				                                     message?.Payload,
				                                     ex);
			}
		}

		#endregion

		#region Implementation registry

		private static readonly Type[] ImplementationConstructorParameters =
			{typeof(TDSTokenStreamMessage)};

		private static Func<TDSTokenStreamMessage, TDSToken> MakeConstructor(Type t)
		{
			// Get default constructor
			var ci = t.GetConstructor(
				         BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
				         null,
				         ImplementationConstructorParameters,
				         null) ??
			         throw new InvalidOperationException(
				         $"Unable to find constructor from TDSTokenStreamMessage for type {t.FullName}");

			// Generate IL function to invoke the default constructor
			var dm = new DynamicMethod("_dynamic_constructor", t, ImplementationConstructorParameters, t);
			var ilg = dm.GetILGenerator();
			ilg.Emit(OpCodes.Ldarg_0);
			ilg.Emit(OpCodes.Newobj, ci);
			ilg.Emit(OpCodes.Ret);
			return (Func<TDSTokenStreamMessage, TDSToken>)dm.CreateDelegate(
				typeof(Func<TDSTokenStreamMessage, TDSToken>));
		}

		private static readonly Dictionary<TDSTokenType, Func<TDSTokenStreamMessage, TDSToken>>
			ConcreteTypeConstructors =
				(
					from cls in Assembly.GetExecutingAssembly().GetTypes()
					where !cls.IsAbstract && typeof(TDSToken).IsAssignableFrom(cls)
					select cls
				).ToDictionary(t => MakeConstructor(t)(null).TokenId, MakeConstructor);

		#endregion
	}
}
