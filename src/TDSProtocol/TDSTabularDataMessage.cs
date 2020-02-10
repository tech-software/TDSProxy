using System;

namespace TDSProtocol
{
	public class TDSTabularDataMessage : TDSTokenStreamMessage
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		public override TDSMessageType MessageType => TDSMessageType.TabularResult;
	}
}
