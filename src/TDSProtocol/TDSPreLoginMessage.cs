using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace TDSProtocol
{
	public class TDSPreLoginMessage : TDSMessage
	{
		#region Log4Net

		static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		#endregion

		public override TDSMessageType MessageType => TDSMessageType.PreLogin;

		#region Version

		public struct VersionInfo : IEquatable<VersionInfo>
		{
			public uint Version { get; set; }
			public ushort SubBuild { get; set; }

			#region Equality testing

			public bool Equals(VersionInfo other)
			{
				return Version == other.Version && SubBuild == other.SubBuild;
			}

			public override bool Equals(object obj)
			{
				return obj is VersionInfo versionInfo && Equals(versionInfo);
			}

			[SuppressMessage("ReSharper",
				"NonReadonlyMemberInGetHashCode",
				Justification =
					"Setters necessary for reading message, values won't change between calls to GetHashCode")]
			public override int GetHashCode()
			{
				return Version.GetHashCode() ^ (13 * SubBuild.GetHashCode());
			}

			public static bool operator ==(VersionInfo x, VersionInfo y)
			{
				return x.Equals(y);
			}

			public static bool operator !=(VersionInfo x, VersionInfo y)
			{
				return !x.Equals(y);
			}

			#endregion
		}

		private VersionInfo? _version;

		public VersionInfo? Version
		{
			get => _version;
			set
			{
				Payload = null;
				_version = value;
			}
		}

		#endregion

		#region Encryption

		public enum EncryptionEnum : byte
		{
			Off = 0x00,
			On = 0x01,
			NotSupported = 0x02,
			Required = 0x03
		}

		private EncryptionEnum? _encryption;

		public EncryptionEnum? Encryption
		{
			get => _encryption;
			set
			{
				Payload = null;
				_encryption = value;
			}
		}

		#endregion

		#region InstValidity

		// null-terminated multi-byte character string. MULTI-BYTE!!!
		private byte[] _instValidity;

		public byte[] InstValidity
		{
			get => _instValidity;
			set
			{
				Payload = null;
				_instValidity = value;
			}
		}

		#endregion

		#region ThreadId

		private uint? _threadId;

		public uint? ThreadId
		{
			get => _threadId;
			set
			{
				Payload = null;
				_threadId = value;
			}
		}

		#endregion

		#region Mars

		[PublicAPI]
		public enum MarsEnum : byte
		{
			Off = 0x00,
			On = 0x01
		}

		private MarsEnum? _mars;

		public MarsEnum? Mars
		{
			get => _mars;
			set
			{
				Payload = null;
				_mars = value;
			}
		}

		#endregion

		#region TraceId

		public struct TraceIdData : IEquatable<TraceIdData>
		{
			public Guid ConnId { get; set; }

			private byte[] _activityId;

			public byte[] ActivityId
			{
				get => _activityId;
				set
				{
					if (null == value)
						throw new ArgumentNullException();
					if (value.Length != 0 && value.Length != 20)
						throw new ArgumentException("ActivityId must be 20 bytes");
					_activityId = value;
				}
			}

			#region Equality testing

			public bool Equals(TraceIdData other)
			{
				// NB: since TraceIdData is a struct, other *cannot* be null
				return ConnId == other.ConnId &&
				       (ReferenceEquals(ActivityId, other.ActivityId) ||
				        (null != ActivityId && null != other.ActivityId && ActivityId.SequenceEqual(other.ActivityId)));
			}

			public override bool Equals(object obj)
			{
				return obj is TraceIdData traceIdData && Equals(traceIdData);
			}

			public override int GetHashCode()
			{
				// ReSharper disable once NonReadonlyMemberInGetHashCode
				// Nothing's going to change the property between calls but setter is needed for parsing messages
				int hash = ConnId.GetHashCode();
				if (null != ActivityId)
				{
					foreach (var b in ActivityId)
						hash = (hash * 17) ^ b.GetHashCode();
				}

				return hash;
			}

			public static bool operator ==(TraceIdData x, TraceIdData y)
			{
				return x.Equals(y);
			}

			public static bool operator !=(TraceIdData x, TraceIdData y)
			{
				return !x.Equals(y);
			}

			#endregion
		}

		private TraceIdData? _traceId;

		public TraceIdData? TraceId
		{
			get => _traceId;
			set
			{
				Payload = null;
				_traceId = value;
			}
		}

		#endregion

		#region FedAuthRequired

		[PublicAPI]
		public enum FedAuthRequiredEnum : byte
		{
			ClientSupportsSspiOrFederation = 0x01,

			ServerSupportsSspiOrFederation = 0x00,
			ServerDoesNotSupportSspi = 0x01
		}

		private FedAuthRequiredEnum? _fedAuthRequired;

		public FedAuthRequiredEnum? FedAuthRequired
		{
			get => _fedAuthRequired;
			set
			{
				Payload = null;
				_fedAuthRequired = value;
			}
		}

		#endregion

		#region Nonce

		private byte[] _nonce;

		public byte[] Nonce
		{
			get => _nonce;
			set
			{
				if (null != value && 32 != value.Length)
					throw new ArgumentException("Nonce must be 32 bytes");
				Payload = null;
				_nonce = value;
			}
		}

		#endregion

		#region SslPayload

		public byte[] SslPayload
		{
			get => Payload;
			set => Payload = value;
		}

		#endregion

		#region OptionData

		public enum OptionToken : byte
		{
			Version = 0x00,
			Encryption = 0x01,
			InstOpt = 0x02,
			ThreadId = 0x03,
			Mars = 0x04,
			TraceId = 0x05,
			FedAuthRequired = 0x06,
			Nonce = 0x07,

			Terminator = 0xff
		}

		private class OptionData
		{
			public OptionToken Token { get; set; }
			public ushort Offset { get; set; }
			public ushort Length { get; set; }
		}

		#endregion

		#region ReadOption

		private OptionData ReadOption(BinaryReader br)
		{
			if (null == br)
				throw new ArgumentNullException(nameof(br));
			var retVal = new OptionData
			             {
				             Token = (OptionToken)br.ReadByte()
			             };
			if (retVal.Token == OptionToken.Terminator) // Terminator
				return retVal;
			if (br.BaseStream.Position + 3 > br.BaseStream.Length)
				throw new TDSInvalidMessageException("Attempted to read option longer than remainder of payload",
				                                     MessageType,
				                                     Payload);
			retVal.Offset = br.ReadBigEndianUInt16();
			retVal.Length = br.ReadBigEndianUInt16();
			if (retVal.Offset + retVal.Length > br.BaseStream.Length)
				throw new TDSInvalidMessageException("Option Data past end of message", MessageType, Payload);
			return retVal;
		}

		#endregion

		#region WriteOption

		private void WriteOption(BinaryWriter bw, OptionData opt)
		{
			bw.Write((byte)opt.Token);
			if (opt.Token == OptionToken.Terminator)
				return;
			bw.WriteBigEndian(opt.Offset);
			bw.WriteBigEndian(opt.Length);
		}

		#endregion

		#region GeneratePayload

		private void AddToken(List<OptionData> list, OptionToken token, ref ushort offset, ushort length)
		{
			var opt = new OptionData {Token = token, Offset = offset, Length = length};
			list.Add(opt);
			offset += length;
		}

		protected internal override void GeneratePayload()
		{
			var options = new List<OptionData>();
			// Initially, option.Offset will be relative to end of option tokens, we'll come back and offset by the length of the token data later
			ushort offset = 0;
			if (null != Version)
				AddToken(options, OptionToken.Version, ref offset, 6);
			if (null != Encryption)
				AddToken(options, OptionToken.Encryption, ref offset, 1);
			if (null != InstValidity)
				AddToken(options, OptionToken.InstOpt, ref offset, (ushort)InstValidity.Length);
			if (null != ThreadId)
				AddToken(options, OptionToken.ThreadId, ref offset, 4);
			if (null != Mars)
				AddToken(options, OptionToken.Mars, ref offset, 1);
			if (null != TraceId)
				AddToken(options, OptionToken.TraceId, ref offset, 36);
			if (null != FedAuthRequired)
				AddToken(options, OptionToken.FedAuthRequired, ref offset, 1);
			if (null != Nonce)
				AddToken(options, OptionToken.Nonce, ref offset, 1);

			AddToken(options, OptionToken.Terminator, ref offset, 0);

			// Calculate the length of the token data and bump the offsets
			var initOffset =
				(ushort)((options.Count * 5) - 4); // 5 bytes per token, except the final Terminator is only 1 byte
			foreach (var o in options)
				o.Offset += initOffset;

			Payload = new byte[initOffset + options.Sum(o => o.Length)];
			using (var ms = new MemoryStream(Payload))
			using (var bw = new BinaryWriter(ms))
			{
				foreach (var o in options)
					WriteOption(bw, o);
				if (null != Version)
				{
					bw.WriteBigEndian(Version.Value.Version);
					bw.Write(Version.Value.SubBuild);
				}

				if (null != Encryption)
					bw.Write((byte)Encryption.Value);
				if (null != InstValidity)
					bw.Write(InstValidity);
				if (null != ThreadId)
					bw.Write(ThreadId.Value);
				if (null != Mars)
					bw.Write((byte)Mars.Value);
				if (null != TraceId)
				{
					bw.Write(TraceId.Value.ConnId.ToByteArray());
					bw.Write(TraceId.Value.ActivityId);
				}

				if (null != FedAuthRequired)
					bw.Write((byte)FedAuthRequired.Value);
				if (null != Nonce)
					bw.Write(Nonce);
			}
		}

		#endregion

		#region InterpretPayload

		protected internal override void InterpretPayload()
		{
			if (null == Payload)
				throw new InvalidOperationException("Attempted to interpret payload, but no payload to interpret");

			if (Enum.IsDefined(typeof(SslPacketType), Payload[0]))
				// Looks like it's an SSL packet
				return;

			using (var ms = new MemoryStream(Payload))
			using (var br = new BinaryReader(ms))
			{
				var options = new List<OptionData>();
				OptionData opt;
				while ((opt = ReadOption(br)).Token != OptionToken.Terminator)
					options.Add(opt);

				// NOTE: setting fields NOT properties here because setting properties wipes out Payload
				foreach (var o in options)
				{
					ms.Position = o.Offset;
					switch (o.Token)
					{
					case OptionToken.Version:
						if (o.Length < 6)
							throw new TDSInvalidMessageException(
								$"version option length ({o.Length}) was less than 6",
								MessageType,
								Payload);
						if (o.Length > 6)
							log.InfoFormat("Additional data in PRELOGIN version option, length was {0}, expected 6",
							               o.Length);
						var version = br.ReadBigEndianUInt32();
						var subBuild = br.ReadUInt16();
						_version = new VersionInfo {Version = version, SubBuild = subBuild};
						break;
					case OptionToken.Encryption:
						if (o.Length < 1)
							throw new TDSInvalidMessageException("encryption option contained no data",
							                                     MessageType,
							                                     Payload);
						if (o.Length > 1)
							log.InfoFormat("Additional data in PRELOGIN encryption option, length was {0}, expected 1",
							               o.Length);
						_encryption = (EncryptionEnum)br.ReadByte();
						break;
					case OptionToken.InstOpt:
						_instValidity = br.ReadBytes(o.Length);
						break;
					case OptionToken.ThreadId:
						if (o.Length == 0)
							_threadId = null;
						else
						{
							if (o.Length < 4)
								throw new TDSInvalidMessageException(
									$"ThreadId option length ({o.Length}) was less than 4",
									MessageType,
									Payload);
							if (o.Length > 4)
								log.InfoFormat(
									"Additional data in PRELOGIN ThreadId option, length was {0}, expected 4",
									o.Length);
							_threadId = br.ReadUInt32();
						}

						break;
					case OptionToken.Mars:
						if (o.Length < 1)
							throw new TDSInvalidMessageException("MARS option contained no data", MessageType, Payload);
						if (o.Length > 1)
							log.InfoFormat("Additional data in PRELOGIN MARS option, length was {0}, expected 1",
							               o.Length);
						_mars = (MarsEnum)br.ReadByte();
						break;
					case OptionToken.TraceId:
						if (o.Length == 0)
							break;
						if (o.Length < 36)
							throw new TDSInvalidMessageException(
								$"TraceId option length ({o.Length}) was less than 36",
								MessageType,
								Payload);
						if (o.Length > 36)
							log.InfoFormat("Additional data in PRELOGIN TraceId option, length was {0}, expected 36",
							               o.Length);
						var connId = new Guid(br.ReadBytes(16));
						var activityId = br.ReadBytes(20);
						_traceId = new TraceIdData {ConnId = connId, ActivityId = activityId};
						break;
					case OptionToken.FedAuthRequired:
						if (o.Length < 1)
							throw new TDSInvalidMessageException("FedAuthRequired option contained no data",
							                                     MessageType,
							                                     Payload);
						if (o.Length > 1)
							log.InfoFormat(
								"Additional data in PRELOGIN FedAuthRequired option, length was {0}, expected 1",
								o.Length);
						_fedAuthRequired = (FedAuthRequiredEnum)br.ReadByte();
						break;
					case OptionToken.Nonce:
						if (o.Length < 32)
							throw new TDSInvalidMessageException(
								$"Nonce option length ({o.Length}) was less than 32",
								MessageType,
								Payload);
						if (o.Length > 32)
							log.InfoFormat("Additional data in PRELOGIN Nonce option, length was {0}, expected 32",
							               o.Length);
						_nonce = br.ReadBytes(32);
						break;
					default:
						log.InfoFormat(
							"Ignoring unknown PRELOGIN option {0} with data {1}",
							o.Token,
							string.Join(" ", br.ReadBytes(o.Length).Select(b => b.ToString("X2"))));
						break;
					}
				}
			}
		}

		#endregion
	}
}
