using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConnection
{
	class Program
	{
		static void Main(string[] args)
		{
			var cn = new OleDbConnection("Provider=SQLNCLI11;Server=pc-mattw.techsoftwareinc.com;Database=IRBM_Dev;Uid=dev\\mattw;Pwd=dev;MARS_Connection=no");
			//var cn = new OdbcConnection("Driver={SQL Server Native Client 11.0};Server=pc-mattw.techsoftwareinc.com;Database=IRBM_Dev;Uid=dev\\mattw;Pwd=dev;MARS_Connection=no");
			//var cn = new SqlConnection("Server=pc-mattw.techsoftwareinc.com;Database=IRBM_Dev;User id=dev\\mattw;Password=dev;MultipleActiveResultSets=false");
			cn.Open();
			while (true)
			{
				var cmd = cn.CreateCommand();
				cmd.CommandText = "SELECT * FROM Note";
				var dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					for (int i = 0; i < dr.FieldCount; i++)
						Console.Write("{0}\t", dr.GetValue(i));
					Console.WriteLine();
				}
				//Console.ReadKey();
			}
		}
	}
}
