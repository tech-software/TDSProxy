using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TDSProtocol;

namespace TDSProtocolTests
{
	[TestClass]
	public class BinaryWriterExtensionsUnitTests
	{
		[TestMethod]
		public void TestInt16Positive()
		{
			short value = 0x1234;
			byte[] expected = new byte[] { 0x12, 0x34 };

			byte[] actual = new byte[2];
			using (var ms = new MemoryStream(actual))
			using (var bw = new BinaryWriter(ms))
				bw.WriteBigEndian(value);

			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestInt16Negative()
		{
			short value = -0x5433; // = 0xABCD
			byte[] expected = new byte[] { 0xAB, 0xCD };

			byte[] actual = new byte[2];
			using (var ms = new MemoryStream(actual))
			using (var bw = new BinaryWriter(ms))
				bw.WriteBigEndian(value);

			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestUInt16Small()
		{
			ushort value = 0x2345;
			byte[] expected = new byte[] { 0x23, 0x45 };

			byte[] actual = new byte[2];
			using (var ms = new MemoryStream(actual))
			using (var bw = new BinaryWriter(ms))
				bw.WriteBigEndian(value);

			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestUInt16Large()
		{
			ushort value = 0xBCDE;
			byte[] expected = new byte[] { 0xBC, 0xDE };

			byte[] actual = new byte[2];
			using (var ms = new MemoryStream(actual))
			using (var bw = new BinaryWriter(ms))
				bw.WriteBigEndian(value);

			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestInt32Positive()
		{
			int value = 0x1A253A45;
			byte[] expected = new byte[] { 0x1A, 0x25, 0x3A, 0x45 };

			byte[] actual = new byte[4];
			using (var ms = new MemoryStream(actual))
			using (var bw = new BinaryWriter(ms))
				bw.WriteBigEndian(value);

			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestInt32Negative()
		{
			int value = -0x5EAD5CAC; // = 0xA152A354
			byte[] expected = new byte[] { 0xA1, 0x52, 0xA3, 0x54 };

			byte[] actual = new byte[4];
			using (var ms = new MemoryStream(actual))
			using (var bw = new BinaryWriter(ms))
				bw.WriteBigEndian(value);

			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestUInt32Small()
		{
			uint value = 0x152A354A;
			byte[] expected = new byte[] { 0x15, 0x2A, 0x35, 0x4A };

			byte[] actual = new byte[4];
			using (var ms = new MemoryStream(actual))
			using (var bw = new BinaryWriter(ms))
				bw.WriteBigEndian(value);

			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestUInt32Large()
		{
			uint value = 0xA453A251;
			byte[] expected = new byte[] { 0xA4, 0x53, 0xA2, 0x51 };

			byte[] actual = new byte[4];
			using (var ms = new MemoryStream(actual))
			using (var bw = new BinaryWriter(ms))
				bw.WriteBigEndian(value);

			CollectionAssert.AreEqual(expected, actual);
		}
	}
}
