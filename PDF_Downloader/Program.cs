using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;

public class Program
{
	public static void Main()
	{
		var path = "demo.xlsx";
		WebClient webClient = new WebClient();

	
		// query dynamic
		{
			var rows = MiniExcel.Query(path,useHeaderRow: true).ToList();
			Console.WriteLine(rows[0].A); //Github
			Console.WriteLine(rows[0].B); //2021-01-01 12:00:00 AM
			Console.WriteLine(rows[1].A); //Microsoft
			Console.WriteLine(rows[1].B); //2021-02-01 12:00:00 AM		
		}
		
		// or by stream
		using (var stream = File.OpenRead(path))
		{
			var rows = stream.Query(useHeaderRow: true).ToList();
			Console.WriteLine(rows[0].A); //Github
			Console.WriteLine(rows[0].B); //2021-01-01 12:00:00 AM
			Console.WriteLine(rows[1].A); //Microsoft
			Console.WriteLine(rows[1].B); //2021-02-01 12:00:00 AM		
		}

		// query type mapping
		using (var stream = File.OpenRead(path))
		{
			var rows = stream.Query<Demo>().ToList();
			var rowNumber = rows.Count;
				
			for (int i = 1; i < rowNumber; i++)
			{
				string web = rows[i].A;
				webClient.DownloadFile(rows[i].A, i.ToString());
				Console.WriteLine(rows[i].A);
				Console.WriteLine(rows[i].B);
			}
			Console.WriteLine(rows[0].A); //Github
			Console.WriteLine(rows[0].B); //2021-01-01 12:00:00 AM
			Console.WriteLine(rows[1].A); //Microsoft
			Console.WriteLine(rows[1].B); //2021-02-01 12:00:00 AM			
		}
	}

	public class Demo
	{
		[ExcelColumnIndex("AL")]
		public string A { get; set; }
		[ExcelColumnIndex("AM")]
		public string B { get; set; }
	}
}
