using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public static class BinaryWriterExtensions
	{
		public static void WriteBigEndian(this BinaryWriter writer, short value)
		{
			writer.Write(new byte[] { (byte)(value >> 8), (byte)value });
		}

		public static void WriteBigEndian(this BinaryWriter writer, ushort value)
		{
			writer.Write(new byte[] { (byte)(value >> 8), (byte)value });
		}

		public static void WriteBigEndian(this BinaryWriter writer, int value)
		{
			writer.Write(new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
		}

		public static void WriteBigEndian(this BinaryWriter writer, uint value)
		{
			writer.Write(new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
		}

		public static void WriteUnicodeBytes(this BinaryWriter writer, string value)
		{
			if (null != value)
				writer.Write(Unicode.Instance.GetBytes(value));
		}

		public static void WriteBVarchar(this BinaryWriter writer, string value)
		{
			if (value?.Length > byte.MaxValue)
				throw new ArgumentException($"B_VARCHAR strings must not be longer than {byte.MaxValue} characters", nameof(value));

			if (null == value)
				writer.Write((byte)0);
			else
			{
				writer.Write((byte)value.Length);
				writer.WriteUnicodeBytes(value);
			}
		}

		public static void WriteUsVarchar(this BinaryWriter writer, string value)
		{
			if (value?.Length > ushort.MaxValue)
				throw new ArgumentException($"US_VARCHAR strings must not be longer than {ushort.MaxValue} characters", nameof(value));

			if (null == value)
				writer.WriteBigEndian((ushort)0);
			else
			{
				writer.Write((ushort)value.Length);
				writer.WriteUnicodeBytes(value);
			}
		}

		public static void WriteObfuscatedPassword(this BinaryWriter writer, string password)
		{
			if (null != password)
			{
				var passwordBytes = Unicode.Instance.GetBytes(password);
				for (var idx = 0; idx < passwordBytes.Length; idx++)
				{
					byte b = passwordBytes[idx];
					passwordBytes[idx] = (byte)(((b >> 4) | (b << 4)) ^ 0xA5);
				}
				writer.Write(passwordBytes);
			}
		}
	}
}
