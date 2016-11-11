using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public abstract class TDSToken
	{
		#region TdsVersion
		[ThreadStatic]
		private static uint _tdsVersion;
		/// <summary>
		/// TdsVersion for the current message processing. Stored in a ThreadStatic - ***DO NOT RELY ON THIS SURVIVNG "await"***
		/// </summary>
		public static uint TdsVersion
		{
			get { return _tdsVersion; }
			set { _tdsVersion = value; }
		}
		#endregion

		protected internal readonly TDSTokenStreamMessage Message;

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

#if false
		#region ReadFromBinaryReader
		protected abstract void ReadFromBinaryReader(BinaryReader br);

		protected internal static TDSToken ReadFromBinaryReader(TDSTokenStreamMessage message, BinaryReader br)
		{
			var tokenId = (TDSTokenType)br.ReadByte();
			var token = _concreteTypeConstructors[tokenId](message);
			token.ReadFromBinaryReader(br);
			return token;
		}
		#endregion
#endif

		#region Implementation registry

		private static readonly Type[] _implementationConstructorParameters = new Type[] { typeof(TDSTokenStreamMessage) };
		private static Func<TDSTokenStreamMessage, TDSToken> MakeConstructor(Type t)
		{
			// Get default constructor
			var ci = t.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
				null,
				_implementationConstructorParameters,
				null);

			// Generate IL function to invoke the default constructor
			var dm = new DynamicMethod("_dynamic_constructor", t, _implementationConstructorParameters, t);
			var ilg = dm.GetILGenerator();
			ilg.Emit(OpCodes.Ldarg_0);
			ilg.Emit(OpCodes.Newobj, ci);
			ilg.Emit(OpCodes.Ret);
			return (Func<TDSTokenStreamMessage, TDSToken>)dm.CreateDelegate(typeof(Func<TDSTokenStreamMessage, TDSToken>));
		}

		private readonly static Dictionary<TDSTokenType, Func<TDSTokenStreamMessage, TDSToken>> _concreteTypeConstructors =
			(
				from cls in Assembly.GetExecutingAssembly().GetTypes()
				where !cls.IsAbstract && typeof(TDSToken).IsAssignableFrom(cls)
				select cls
			).ToDictionary(t => MakeConstructor(t)(null).TokenId, new Func<Type, Func<TDSTokenStreamMessage, TDSToken>>(MakeConstructor));

		#endregion
	}
}
