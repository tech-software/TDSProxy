using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TDSProtocol;

namespace TDSProtocolTests
{
	[TestClass]
	public class TDSPreLoginMessageTests
	{
		[TestMethod]
		public void TestInterpretPayloadSimple()
		{
			TDSPreLoginMessage expected =
				new TDSPreLoginMessage
				{
					Version = new TDSPreLoginMessage.VersionInfo { Version = 0x09000000, SubBuild = 0x0000 },
					Encryption = TDSPreLoginMessage.EncryptionEnum.On,
					InstValidity = new byte[] { 0x00 },
					ThreadId = 0x00000DB8,
					Mars = TDSPreLoginMessage.MarsEnum.On
				};


			TDSPreLoginMessage actual = new TDSPreLoginMessage();
			actual.Payload = new byte[]
			{
				0x00, 0x00, 0x1A, 0x00, 0x06, 0x01, 0x00, 0x20, 0x00, 0x01, 0x02, 0x00, 0x21, 0x00, 0x01, 0x03,
				0x00, 0x22, 0x00, 0x04, 0x04, 0x00, 0x26, 0x00, 0x01, 0xFF, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x01, 0x00, 0xB8, 0x0D, 0x00, 0x00, 0x01
			};
			actual.InterpretPayload();

			Assert.AreEqual(expected.Version, actual.Version);
			Assert.AreEqual(expected.Encryption, actual.Encryption);
			EnumerableAssert.AreEqual(expected.InstValidity, actual.InstValidity);
			Assert.AreEqual(expected.ThreadId, actual.ThreadId);
			Assert.AreEqual(expected.Mars, actual.Mars);
			Assert.AreEqual(expected.TraceId, actual.TraceId);
			Assert.AreEqual(expected.FedAuthRequired, actual.FedAuthRequired);
			EnumerableAssert.AreEqual(expected.Nonce, actual.Nonce);
			EnumerableAssert.AreEqual(expected.SslPayload, actual.SslPayload);
		}

		[TestMethod]
		public void TestGeneratePayloadSimple()
		{
			TDSPreLoginMessage msg =
				new TDSPreLoginMessage
				{
					Version = new TDSPreLoginMessage.VersionInfo { Version = 0x09000000, SubBuild = 0x0000 },
					Encryption = TDSPreLoginMessage.EncryptionEnum.On,
					InstValidity = new byte[] { 0x00 },
					ThreadId = 0x00000DB8,
					Mars = TDSPreLoginMessage.MarsEnum.On
				};
			msg.GeneratePayload();

			var expected = new byte[]
			{
				0x00, 0x00, 0x1A, 0x00, 0x06, 0x01, 0x00, 0x20, 0x00, 0x01, 0x02, 0x00, 0x21, 0x00, 0x01, 0x03,
				0x00, 0x22, 0x00, 0x04, 0x04, 0x00, 0x26, 0x00, 0x01, 0xFF, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x01, 0x00, 0xB8, 0x0D, 0x00, 0x00, 0x01
			};
			var actual = msg.Payload;

			EnumerableAssert.AreEqual(expected, actual);
		}
	}
}
