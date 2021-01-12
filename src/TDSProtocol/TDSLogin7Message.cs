using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace TDSProtocol
{
	public class TDSLogin7Message : TDSMessage
	{
		#region Log4Net

		static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		#endregion

		public override TDSMessageType MessageType => TDSMessageType.Login7;

		#region TdsVersion

		private uint _tdsVersion;

		public uint TdsVersion
		{
			get => _tdsVersion;
			set
			{
				Payload = null;
				_tdsVersion = value;
			}
		}

		#endregion

		#region PacketSize

		private uint _packetSize;

		public uint PacketSize
		{
			get => _packetSize;
			set
			{
				Payload = null;
				_packetSize = value;
			}
		}

		#endregion

		#region ClientProgVer

		// ReSharper disable IdentifierTypo -- Microsoft's abbreviation, not mine
		private uint _clientProgVer;

		public uint ClientProgVer
			// ReSharper restore IdentifierTypo
		{
			get => _clientProgVer;
			set
			{
				Payload = null;
				_clientProgVer = value;
			}
		}

		#endregion

		#region ClientPid

		private uint _clientPid;

		public uint ClientPid
		{
			get => _clientPid;
			set
			{
				Payload = null;
				_clientPid = value;
			}
		}

		#endregion

		#region ConnectionId

		private uint _connectionId;

		public uint ConnectionId
		{
			get => _connectionId;
			set
			{
				Payload = null;
				_connectionId = value;
			}
		}

		#endregion

		#region OptionFlags1

		[Flags, PublicAPI]
		public enum OptionFlags1Enum : byte
		{
			ByteOrderMask = 0x01,
			ByteOrderX86 = 0x00,
			ByteOrderM68000 = 0x01,

			CharsetMask = 0x02,
			CharsetASCII = 0x00,
			CharsetEBCDIC = 0x02,

			FloatFormatMask = 0x0C,
			FloatFormatIEEE754 = 0x00,
			FloatFormatVAX = 0x04,
			FloatFormatND5000 = 0x08,

			DumpLoadOff = 0x10,

			UseDBNotify = 0x20,

			DatabaseFatal = 0x40,

			SetLangNotify = 0x80
		}

		private OptionFlags1Enum _optionFlags1;

		public OptionFlags1Enum OptionFlags1
		{
			get => _optionFlags1;
			set
			{
				Payload = null;
				_optionFlags1 = value;
			}
		}

		#endregion

		#region OptionFlags2

		[Flags, PublicAPI]
		public enum OptionFlags2Enum : byte
		{
			LanguageFatal = 0x01,

			ODBC = 0x02,

			TransBoundary = 0x04,

			CacheConnect = 0x08,

			UserTypeMask = 0x70,
			UserTypeNormal = 0x00,
			UserTypeServer = 0x10,
			UserTypeRemUser = 0x20,
			// ReSharper disable once IdentifierTypo -- Microsoft's abbreviation, not mine
			UserTypeSQLRepl = 0x30,

			IntegratedSecurity = 0x80
		}

		private OptionFlags2Enum _optionFlags2;

		public OptionFlags2Enum OptionFlags2
		{
			get => _optionFlags2;
			set
			{
				Payload = null;
				_optionFlags2 = value;
			}
		}

		#endregion

		#region TypeFlags

		[Flags, PublicAPI]
		public enum TypeFlagsEnum : byte
		{
			SqlTypeMask = 0x0F,
			SqlTypeDefault = 0x00,
			SqlTypeTSQL = 0x01,

			OLEDB = 0x10,

			ReadOnlyIntent = 0x20
		}

		private TypeFlagsEnum _typeFlags;

		public TypeFlagsEnum TypeFlags
		{
			get => _typeFlags;
			set
			{
				Payload = null;
				_typeFlags = value;
			}
		}

		#endregion

		#region OptionFlags3

		[Flags, PublicAPI]
		public enum OptionFlags3Enum : byte
		{
			ChangePassword = 0x01,

			SendYukonBinaryXml = 0x02,

			UserInstance = 0x04,

			UnknownCollationHandlingMask = 0x08,
			UnknownCollationHandlingRestricted = 0x00,
			UnknownCollationHandlingAnyThatFits = 0x08,

			Extension = 0x10
		}

		private OptionFlags3Enum _optionFlags3;

		public OptionFlags3Enum OptionFlags3
		{
			get => _optionFlags3;
			set
			{
				Payload = null;
				_optionFlags3 = value;
			}
		}

		#endregion

		#region ClientTimeZone

		private int _clientTimeZone;

		public int ClientTimeZone
		{
			get => _clientTimeZone;
			set
			{
				Payload = null;
				_clientTimeZone = value;
			}
		}

		#endregion

		#region ClientLCID

		[Flags, PublicAPI]
		public enum ClientLCIDEnum : uint
		{
			LCIDMask = 0x000fffff,
			IgnoreCase = 0x00100000,
			IgnoreAccent = 0x00200000,
			IgnoreWidth = 0x00400000,
			IgnoreKana = 0x00800000,
			Binary = 0x01000000,
			Binary2 = 0x02000000,
			VersionMask = 0xf0000000
		}

		private ClientLCIDEnum _clientLCID;

		public ClientLCIDEnum ClientLCID
		{
			get => _clientLCID;
			set
			{
				Payload = null;
				_clientLCID = value;
			}
		}

		#endregion

		#region HostName

		private string _hostName;

		public string HostName
		{
			get => _hostName;
			set
			{
				Payload = null;
				_hostName = value;
			}
		}

		#endregion

		#region UserName

		private string _userName;

		public string UserName
		{
			get => _userName;
			set
			{
				Payload = null;
				_userName = value;
			}
		}

		#endregion

		#region Password

		private string _password;

		public string Password
		{
			get => _password;
			set
			{
				Payload = null;
				_password = value;
			}
		}

		#endregion

		#region AppName

		private string _appName;

		public string AppName
		{
			get => _appName;
			set
			{
				Payload = null;
				_appName = value;
			}
		}

		#endregion

		#region ServerName

		private string _serverName;

		public string ServerName
		{
			get => _serverName;
			set
			{
				Payload = null;
				_serverName = value;
			}
		}

		#endregion

		#region FeatureExt

		[PublicAPI]
		public enum FeatureId : byte
		{
			SessionRecovery = 1,
			FedAuth = 2,
			ColumnEncryption = 4,
			GlobalTransactions = 5,
			AzureSqlSupport = 8,
			DataClassification = 9,
			Utf8Support = 10,
			Terminator = 0xff
		}

		public class FeatureOpt
		{
			public FeatureId FeatureId { get; set; }
			public byte[] FeatureData { get; set; }
		}

		private IEnumerable<FeatureOpt> _featureExt;

		public IEnumerable<FeatureOpt> FeatureExt
		{
			get => _featureExt;
			set
			{
				Payload = null;
				_featureExt = value;
			}
		}

		#endregion

		#region ClientInterfaceName

		private string _clientInterfaceName;

		public string ClientInterfaceName
		{
			get => _clientInterfaceName;
			set
			{
				Payload = null;
				_clientInterfaceName = value;
			}
		}

		#endregion

		#region Language

		private string _language;

		public string Language
		{
			get => _language;
			set
			{
				Payload = null;
				_language = value;
			}
		}

		#endregion

		#region Database

		private string _database;

		public string Database
		{
			get => _database;
			set
			{
				Payload = null;
				_database = value;
			}
		}

		#endregion

		#region ClientID

		private byte[] _clientID;

		public byte[] ClientID
		{
			get => _clientID;
			set
			{
				if (null == value)
					throw new ArgumentNullException(nameof(value));
				if (value.Length != 6)
					throw new ArgumentException("ClientID must be 6 bytes long", nameof(value));
				Payload = null;
				_clientID = value;
			}
		}

		#endregion

		#region SSPI

		private byte[] _sspi;

		public byte[] SSPI
		{
			get => _sspi;
			set
			{
				Payload = null;
				_sspi = value;
			}
		}

		#endregion

		#region AttachDBFile

		private string _attachDBFile;

		public string AttachDBFile
		{
			get => _attachDBFile;
			set
			{
				Payload = null;
				_attachDBFile = value;
			}
		}

		#endregion

		#region ChangePassword

		private string _changePassword;

		public string ChangePassword
		{
			get => _changePassword;
			set
			{
				Payload = null;
				_changePassword = value;
			}
		}

		#endregion

		#region GeneratePayload method

		protected internal override void GeneratePayload()
		{
			// Calculate payload length
			// ReSharper disable CommentTypo -- Microsoft's identifiers, not mine
			var fixedPayloadLength =
				(6 * 4) + // Length, TdsVersion, PacketSize, ClientProgVer, ClientPID, ConnectionID
				4 + // OptionFlags1, OptionFlags2, TypeFlags, OptionFlags3
				(2 * 4) + // ClientTimZone, ClientLCID
				(18 *
				 2) + // ibHostName, cchHostName, ibUserName, cchUserName, ibPassword, cchPassword, ibAppName, cchAppName, ibServerName, cchServerName, ibExtension, cbExtension,
				//         // ibCltIntName, cchCltIntName, ibLanguage, cchLanguage, ibDatabase, cchDatabase
				6 + // ClientID
				(4 * 2); // ibSSPI, cbSSPI, ibAtchDBFile, cchAtchDBFile
			// ReSharper restore CommentTypo
			bool is72OrLater = TdsVersion >= 0x72000000;
			if (is72OrLater)
				fixedPayloadLength += (2 * 2) + 4; // ibChangePassword, cchChangePassword, cbSSPILong
			bool hasExtension = (OptionFlags3 & OptionFlags3Enum.Extension) == OptionFlags3Enum.Extension;
			var dataLength =
				new[]
					{
						HostName, UserName, Password, AppName, ServerName, ClientInterfaceName, Language, Database,
						AttachDBFile, is72OrLater ? ChangePassword : null
					}
					.Sum(s => s.UnicodeByteLength()) + // NOTE: UnicodeByteLength is an extension method that is safe to call on null strings
				(hasExtension ? 4 : 0) +
				(SSPI?.Length ?? 0);
			var featureExtLength = hasExtension && null != FeatureExt
				                       ? FeatureExt.Sum(fe => fe.FeatureData.Length + 5) + 1
				                       : 0;
			byte[] payload = new byte[fixedPayloadLength + dataLength + featureExtLength];

			using (var ms = new MemoryStream(payload))
			using (var bw = new BinaryWriter(ms))
			{
				bw.Write(payload.Length);
				bw.Write(TdsVersion);
				bw.Write(PacketSize);
				bw.Write(ClientProgVer);
				bw.Write(ClientPid);
				bw.Write(ConnectionId);
				bw.Write((byte)OptionFlags1);
				bw.Write((byte)OptionFlags2);
				bw.Write((byte)TypeFlags);
				bw.Write((byte)OptionFlags3);
				bw.Write(ClientTimeZone);
				bw.Write((uint)ClientLCID);

				var dataOffset = (ushort)fixedPayloadLength;

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(HostName) ? 0 : HostName.Length));
				// NOTE: UnicodeByteLength is an extension method that is safe to call on null strings
				dataOffset += (ushort)HostName.UnicodeByteLength();

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(UserName) ? 0 : UserName.Length));
				dataOffset += (ushort)UserName.UnicodeByteLength();

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(Password) ? 0 : Password.Length));
				dataOffset += (ushort)Password.UnicodeByteLength();

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(AppName) ? 0 : AppName.Length));
				dataOffset += (ushort)AppName.UnicodeByteLength();

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(ServerName) ? 0 : ServerName.Length));
				dataOffset += (ushort)ServerName.UnicodeByteLength();

				if (hasExtension)
				{
					bw.Write(dataOffset);
					bw.Write((ushort)4);
					dataOffset += 4;
				}
				else
				{
					bw.Write((ushort)0);
					bw.Write((ushort)0);
				}

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(ClientInterfaceName) ? 0 : ClientInterfaceName.Length));
				dataOffset += (ushort)ClientInterfaceName.UnicodeByteLength();

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(Language) ? 0 : Language.Length));
				dataOffset += (ushort)Language.UnicodeByteLength();

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(Database) ? 0 : Database.Length));
				dataOffset += (ushort)Database.UnicodeByteLength();

				bw.Write(ClientID);

				// NOTE: always put SSPI data last, since it might be too long to be able to represent its end offset in 16 bits
				var attachDBFileBytes = (ushort)AttachDBFile.UnicodeByteLength();
				var changePasswordBytes = (ushort)(is72OrLater ? ChangePassword.UnicodeByteLength() : 0);
				var sspiSkipBytes = attachDBFileBytes + changePasswordBytes;

				bw.Write((ushort)(dataOffset + sspiSkipBytes));
				bw.Write((ushort)(null == SSPI ? 0 : Math.Min(SSPI.Length, 0xFFFF)));
				// NOTE: Don't increase dataOffset since we're going to write AttachDBFile and ChangePassword *before* SSPI

				bw.Write(dataOffset);
				bw.Write((ushort)(string.IsNullOrEmpty(AttachDBFile) ? 0 : AttachDBFile.Length));
				dataOffset += (ushort)AttachDBFile.UnicodeByteLength();

				if (is72OrLater)
				{
					bw.Write(dataOffset);
					bw.Write((ushort)(string.IsNullOrEmpty(ChangePassword) ? 0 : ChangePassword.Length));
					// ReSharper disable once RedundantAssignment -- leave in for future-proofing
					dataOffset += (ushort)ChangePassword.UnicodeByteLength();

					bw.Write(null != SSPI && SSPI.Length > 0xFFFF ? SSPI.Length : 0);
				}

				Debug.Assert(
					ms.Position == fixedPayloadLength,
					"Fixed part does not match expected length",
					"Expected fixed part to be {0} bytes, but wrote {1}",
					fixedPayloadLength,
					ms.Length);

				bw.WriteUnicodeBytes(HostName);
				bw.WriteUnicodeBytes(UserName);
				bw.WriteObfuscatedPassword(Password);
				bw.WriteUnicodeBytes(AppName);
				bw.WriteUnicodeBytes(ServerName);
				if (hasExtension)
					bw.Write(fixedPayloadLength + dataLength);
				bw.WriteUnicodeBytes(ClientInterfaceName);
				bw.WriteUnicodeBytes(Database);
				bw.WriteUnicodeBytes(AttachDBFile);
				if (is72OrLater)
					bw.WriteObfuscatedPassword(ChangePassword);
				if (null != SSPI)
					bw.Write(SSPI);

				Debug.Assert(
					ms.Position == fixedPayloadLength + dataLength,
					"Data length mismatch",
					"Expected fixed part plus data to be {0} bytes, but wrote {1}",
					fixedPayloadLength + dataLength,
					ms.Position);

				if (hasExtension && null != FeatureExt)
				{
					foreach (var feature in FeatureExt)
					{
						bw.Write((byte)feature.FeatureId);
						bw.Write(feature.FeatureData.Length);
						bw.Write(feature.FeatureData);
					}

					bw.Write((byte)FeatureId.Terminator);
				}

				Debug.Assert(
					ms.Length == payload.Length,
					"Packet length mismatch",
					"Expected packet length would be {0} bytes, but wrote {1}",
					payload.Length,
					ms.Length);
			}

			Payload = payload;
		}

		#endregion

		#region InterpretPayload method

		protected internal override void InterpretPayload()
		{
			using (var ms = new MemoryStream(Payload))
			using (var br = new BinaryReader(ms))
			{
				// Read fixed data
				var length = br.ReadInt32();
				if (length != Payload.Length)
					log.WarnFormat("Payload length mismatch - message length = {0} but length field says {1}",
					               Payload.Length,
					               length);

				if (length > Payload.Length)
					throw new TDSInvalidMessageException(
						$"Payload length was {Payload.Length}, but login length field was {length}",
						MessageType,
						Payload);

				// NOTE: setting fields NOT properties here because setting properties wipes out Payload
				_tdsVersion = br.ReadUInt32();
				_packetSize = br.ReadUInt32();
				_clientProgVer = br.ReadUInt32();
				_clientPid = br.ReadUInt32();
				_connectionId = br.ReadUInt32();
				_optionFlags1 = (OptionFlags1Enum)br.ReadByte();
				_optionFlags2 = (OptionFlags2Enum)br.ReadByte();
				_typeFlags = (TypeFlagsEnum)br.ReadByte();
				_optionFlags3 = (OptionFlags3Enum)br.ReadByte();
				_clientTimeZone = br.ReadInt32();
				_clientLCID = (ClientLCIDEnum)br.ReadUInt32();

				var hasExtension = TdsVersion >= 0x74000000 &&
				                   (OptionFlags3 & OptionFlags3Enum.Extension) == OptionFlags3Enum.Extension;
				var is72OrLater = TdsVersion >= 0x72000000;

				// Read offset/length block
				var hostNameOffset = br.ReadUInt16(); // MUST point to start of data block
				var hostNameLength = br.ReadUInt16();
				var userNameOffset = br.ReadUInt16();
				var userNameLength = br.ReadUInt16();
				var passwordOffset = br.ReadUInt16();
				var passwordLength = br.ReadUInt16();
				var appNameOffset = br.ReadUInt16();
				var appNameLength = br.ReadUInt16();
				var serverNameOffset = br.ReadUInt16();
				var serverNameLength = br.ReadUInt16();
				var extensionOffset = br.ReadUInt16();
				var extensionLength = br.ReadUInt16();
				var cltIntNameOffset = br.ReadUInt16();
				var cltIntNameLength = br.ReadUInt16();
				var languageOffset = br.ReadUInt16();
				var languageLength = br.ReadUInt16();
				var databaseOffset = br.ReadUInt16();
				var databaseLength = br.ReadUInt16();
				_clientID = br.ReadBytes(6);
				var sspiOffset = br.ReadUInt16();
				uint sspiLength =
					// ReSharper disable once CommentTypo
					br.ReadUInt16(); // NOTE: TDS >= 7.2 has a long SSPI length later that's set if this ushort is 0xFFFF
				var attachDbFileOffset = br.ReadUInt16();
				var attachDbFileLength = br.ReadUInt16();
				ushort changePasswordOffset = 0;
				ushort changePasswordLength = 0;
				if (is72OrLater)
				{
					if (hostNameOffset >= ms.Position + 8)
					{
						changePasswordOffset = br.ReadUInt16();
						changePasswordLength = br.ReadUInt16();

						var sspiLong = br.ReadUInt32(); // Read it to skip it, even if we won't use it
						if (sspiLength == 0xFFFF)
							sspiLength = sspiLong;
					}
					else
					{
						log.WarnFormat(
							"Client announced version >= 7.2 but offset/length block not long enough to contain additional fields (hostNameOffset = {0}, current position = {1})",
							hostNameOffset,
							ms.Position);
					}
				}

				var fixedEndOffset = ms.Position;
				if (fixedEndOffset > hostNameOffset)
					throw new TDSInvalidMessageException(
						$"Fixed length part of message is {fixedEndOffset} bytes but ibHostName was at {hostNameOffset}",
						MessageType,
						Payload);
				uint EndOffset(uint startOffset, uint itemLength) => itemLength > 0 ? startOffset + itemLength : 0;
				var dataEndOffset = new[]
				{
					hostNameOffset,
					EndOffset(hostNameOffset, hostNameLength * 2u),
					EndOffset(userNameOffset, userNameLength * 2u),
					EndOffset(passwordOffset, passwordLength * 2u),
					EndOffset(appNameOffset, appNameLength * 2u),
					EndOffset(serverNameOffset, serverNameLength * 2u),
					hasExtension ? EndOffset(extensionOffset, extensionLength) : 0,
					EndOffset(cltIntNameOffset, cltIntNameLength * 2u),
					EndOffset(languageOffset, languageLength * 2u),
					EndOffset(databaseOffset, databaseLength * 2u),
					EndOffset(sspiOffset, sspiLength),
					EndOffset(attachDbFileOffset, attachDbFileLength * 2u),
					EndOffset(changePasswordOffset, changePasswordLength * 2u)
				}.Max();

				if (dataEndOffset > length)
					throw new TDSInvalidMessageException(
						$"'data' block does not fit in message length - block extends to {dataEndOffset} but message length only {length}",
						MessageType,
						Payload);

				// Read data pointed to by offset/length block
				_hostName = 0 == hostNameLength ? null : br.ReadUnicodeAt(hostNameOffset, hostNameLength);
				_userName = 0 == userNameLength ? null : br.ReadUnicodeAt(userNameOffset, userNameLength);
				_password = 0 == passwordLength ? null : br.ReadObfuscatedPassword(passwordOffset, passwordLength);
				_appName = 0 == appNameLength ? null : br.ReadUnicodeAt(appNameOffset, appNameLength);
				_serverName = 0 == serverNameLength ? null : br.ReadUnicodeAt(serverNameOffset, serverNameLength);
				uint featureExtOffset = 0;
				if (hasExtension)
				{
					if (extensionLength < 4)
						throw new TDSInvalidMessageException(
							"Client indicated extension was present but did not allocated enough space for ibFeatureExtLong " +
							$"(cbExtension = {extensionLength}, expected 4)",
							MessageType,
							Payload);

					if (extensionLength > 4)
						log.WarnFormat("cbExtension value was unexpected - actual = {0}, expected = 4",
						               extensionLength);

					if (extensionLength >= 4)
					{
						ms.Position = extensionOffset;
						featureExtOffset = br.ReadUInt32();

						if (featureExtOffset != 0 && featureExtOffset < fixedEndOffset)
							throw new TDSInvalidMessageException(
								"ibFeatureExt pointed to within fixed portion of message " +
								$"(ibFeatureExt = {featureExtOffset}, end of fixed data at {fixedEndOffset})",
								MessageType,
								Payload);

						if (featureExtOffset >= hostNameOffset && featureExtOffset < dataEndOffset)
							throw new TDSInvalidMessageException(
								"ibFeatureExt pointed to within data block " +
								$"(ibFeatureExt = {featureExtOffset}, data from {hostNameOffset} to {dataEndOffset})",
								MessageType,
								Payload);
					}
				}

				_clientInterfaceName =
					0 == cltIntNameLength ? null : br.ReadUnicodeAt(cltIntNameOffset, cltIntNameLength);
				_language = 0 == languageLength ? null : br.ReadUnicodeAt(languageOffset, languageLength);
				_database = 0 == databaseLength ? null : br.ReadUnicodeAt(databaseOffset, databaseLength);
				if (sspiLength > 0)
				{
					ms.Position = sspiOffset;
					_sspi = br.ReadBytes((int)sspiLength);
				}
				else
					_sspi = null;

				_attachDBFile = 0 == attachDbFileLength
					                ? null
					                : br.ReadUnicodeAt(attachDbFileOffset, attachDbFileLength);
				_changePassword = 0 == changePasswordLength
					                  ? null
					                  : br.ReadObfuscatedPassword(changePasswordOffset, changePasswordLength);

				if (hasExtension && featureExtOffset > 0)
				{
					ms.Position = featureExtOffset;
					var featureExt = new List<FeatureOpt>();
					while (true)
					{
						if (ms.Position >= length)
							throw new TDSInvalidMessageException("Terminator not found in FeatureExt block",
							                                     MessageType,
							                                     Payload);

						var feature = new FeatureOpt {FeatureId = (FeatureId)br.ReadByte()};
						if (feature.FeatureId == FeatureId.Terminator)
							break; // Terminator indicates we're done.

						if (ms.Position + 4 > length)
							throw new TDSInvalidMessageException(
								$"End of message came in middle of feature header for feature {feature.FeatureId}",
								MessageType,
								Payload);

						var featureLength = br.ReadInt32();
						if (ms.Position + featureLength > length)
							throw new TDSInvalidMessageException(
								$"End of message came in middle of feature data for feature {feature.FeatureId}",
								MessageType,
								Payload);

						feature.FeatureData = br.ReadBytes(featureLength);
						featureExt.Add(feature);
					}

					if (featureExtOffset < hostNameOffset && ms.Position > hostNameOffset)
						throw new TDSInvalidMessageException(
							"FeatureExt block overlapped data block, " +
							$"FeatureExt from {featureExtOffset} to {ms.Position}, data starts at {hostNameOffset}",
							MessageType,
							Payload);

					FeatureExt = featureExt;
				}
				else
					ms.Position = dataEndOffset;

				if (ms.Position != length)
				{
					if (bool.Parse(ConfigurationManager.AppSettings["DumpOnLengthMismatch"] ?? "false"))
					{
						var dumpDir = ConfigurationManager.AppSettings["DumpDirectory"];
						var dumpId = Guid.NewGuid().ToString("D") + ".log";
						var file = Path.Combine(dumpDir, dumpId);
						File.WriteAllBytes(file, Payload);
						log.Debug($"Dumped as {file}");
					}

					log.WarnFormat("Login message length was {0} but read {1} bytes", length, ms.Position);
				}
			}
		}

		#endregion
	}
}
