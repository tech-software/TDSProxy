using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TDSProtocol
{
	public class TDSFeatureExtAckToken : TDSToken
	{
		public TDSFeatureExtAckToken(TDSTokenStreamMessage message) : base(message)
		{
		}

		public override TDSTokenType TokenId => TDSTokenType.FeatureExtAck;

		#region FeatureAckOpts

		public class FeatureAckOpt
		{
			public TDSLogin7Message.FeatureId FeatureId { get; set; }
			public byte[] FeatureAckData { get; set; }
		}

		private IEnumerable<FeatureAckOpt> _featureAckOpts = Enumerable.Empty<FeatureAckOpt>();

		public IEnumerable<FeatureAckOpt> FeatureAckOpts
		{
			get => _featureAckOpts;
			set
			{
				Message.Payload = null;
				_featureAckOpts = value ?? Enumerable.Empty<FeatureAckOpt>();
			}
		}

		#endregion

		#region WriteBodyToBinaryWriter

		protected override void WriteBodyToBinaryWriter(BinaryWriter bw)
		{
			foreach (var fo in FeatureAckOpts)
			{
				bw.Write((byte)fo.FeatureId);
				if (fo.FeatureId == TDSLogin7Message.FeatureId.Terminator)
					return;
				if (fo.FeatureAckData?.Length > 0)
				{
					bw.Write(fo.FeatureAckData.Length);
					bw.Write(fo.FeatureAckData);
				}
				else
				{
					bw.Write(0);
				}
			}

			bw.Write((byte)TDSLogin7Message.FeatureId.Terminator);
		}

		#endregion

		#region ReadFromBinaryReader

		protected override int ReadFromBinaryReader(BinaryReader br)
		{
			var opts = new List<FeatureAckOpt>();

			int length = 0;

			while (true)
			{
				var fo = new FeatureAckOpt();
				int len;

				try
				{
					fo.FeatureId = (TDSLogin7Message.FeatureId)br.ReadByte();
					if (fo.FeatureId == TDSLogin7Message.FeatureId.Terminator)
					{
						opts.Add(fo);
						_featureAckOpts = opts;
						return length + 1;
					}

					len = br.ReadInt32();
					fo.FeatureAckData = br.ReadBytes(len);
				}
				catch (EndOfStreamException)
				{
					throw new TDSInvalidMessageException("Attempted to read longer than payload",
					                                     Message.MessageType,
					                                     Message.Payload);
				}

				opts.Add(fo);
				length += len + 3;
			}
		}

		#endregion
	}
}
