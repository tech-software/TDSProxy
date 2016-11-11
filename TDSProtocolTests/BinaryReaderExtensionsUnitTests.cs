using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TDSProtocol;

namespace TDSProtocolTests
{
	[TestClass]
	public class BinaryReaderExtensionsUnitTests
	{
		[TestMethod]
		public void TestInt16Positive()
		{
			byte[] value = { 0x43, 0x21 };
			short expected = 0x4321;

			short actual;
			using (var ms = new MemoryStream(value))
			using (var br = new BinaryReader(ms))
				actual = br.ReadBigEndianInt16();

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestInt16Negative()
		{
			byte[] value = { 0xED, 0xCB };
			short expected = -0x1235; // = 0xEDCB

			short actual;
			using (var ms = new MemoryStream(value))
			using (var br = new BinaryReader(ms))
				actual = br.ReadBigEndianInt16();

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestUInt16Small()
		{
			byte[] value = { 0x12, 0x34 };
			ushort expected = 0x1234;

			ushort actual;
			using (var ms = new MemoryStream(value))
			using (var br = new BinaryReader(ms))
				actual = br.ReadBigEndianUInt16();

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestUInt16Large()
		{
			byte[] value = { 0xBC, 0xDE };
			ushort expected = 0xBCDE;

			ushort actual;
			using (var ms = new MemoryStream(value))
			using (var br = new BinaryReader(ms))
				actual = br.ReadBigEndianUInt16();

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestInt32Positive()
		{
			byte[] value = { 0x44, 0x33, 0x22, 0x11 };
			int expected = 0x44332211;

			int actual;
			using (var ms = new MemoryStream(value))
			using (var br = new BinaryReader(ms))
				actual = br.ReadBigEndianInt32();

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestInt32Negative()
		{
			byte[] value = { 0xBB, 0xCC, 0xDD, 0xEE };
			int expected = -0x44332212; // = 0xBBCCDDEE

			int actual;
			using (var ms = new MemoryStream(value))
			using (var br = new BinaryReader(ms))
				actual = br.ReadBigEndianInt32();

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestUInt32Small()
		{
			byte[] value = { 0x12, 0x34, 0x23, 0x45 };
			uint expected = 0x12342345;

			uint actual;
			using (var ms = new MemoryStream(value))
			using (var br = new BinaryReader(ms))
				actual = br.ReadBigEndianUInt32();

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void TestUInt32Large()
		{
			byte[] value = { 0xBC, 0xDE, 0xCD, 0xEF };
			uint expected = 0xBCDECDEF;

			uint actual;
			using (var ms = new MemoryStream(value))
			using (var br = new BinaryReader(ms))
				actual = br.ReadBigEndianUInt32();

			Assert.AreEqual(expected, actual);
		}
	}
}
