using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TDSProtocol
{
	public class TDSTabularDataMessage : TDSTokenStreamMessage
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		public override TDSMessageType MessageType
		{
			get { return TDSMessageType.TabularResult; }
		}
	}
}
