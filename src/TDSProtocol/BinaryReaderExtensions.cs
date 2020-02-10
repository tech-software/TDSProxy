using System;
using System.IO;
using JetBrains.Annotations;

namespace TDSProtocol
{
	[PublicAPI]
	public static class BinaryReaderExtensions
	{
		public static short ReadBigEndianInt16(this BinaryReader reader)
		{
			var buffer = reader.ReadBytes(2);
			return (short)((buffer[0] << 8) | buffer[1]);
		}

		public static ushort ReadBigEndianUInt16(this BinaryReader reader)
		{
			var buffer = reader.ReadBytes(2);
			return (ushort)((buffer[0] << 8) | buffer[1]);
		}

		public static int ReadBigEndianInt32(this BinaryReader reader)
		{
			var buffer = reader.ReadBytes(4);
			return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
		}

		public static uint ReadBigEndianUInt32(this BinaryReader reader)
		{
			var buffer = reader.ReadBytes(4);
			return (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
		}

		public static string ReadUnicode(this BinaryReader reader, int charsToRead)
		{
			return new string(Unicode.Instance.GetChars(reader.ReadBytes(charsToRead << 1)));
		}

		public static string ReadUnicodeAt(this BinaryReader reader, int position, int charsToRead)
		{
			reader.BaseStream.Position = position;
			return reader.ReadUnicode(charsToRead);
		}

		public static string ReadObfuscatedPassword(this BinaryReader reader, int position, int charsToRead)
		{
			byte[] passwordBytes = reader.ReadBytes(charsToRead << 1);
			for (int idx = 0; idx < passwordBytes.Length; idx++)
			{
				byte b = (byte)(passwordBytes[idx] ^ 0xA5);
				passwordBytes[idx] = (byte)((b >> 4) | (b << 4));
			}
			return new string(Unicode.Instance.GetChars(passwordBytes));
		}
	}
}
