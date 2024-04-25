using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;

namespace Scripts.Base;

public static class FileUtil
{
	public static void ExportToCsv(string path, List<Dictionary<string, string>> data)
	{
		using var writer = new StreamWriter(path);
		using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

		if (data == null || data.Count == 0)
		{
			Console.WriteLine($"No data to write to CSV file: {path.Substring(path.LastIndexOfAny(new[] { '/', '\\' }))}");
			return;
		}

		var headings = new List<string>(data.First().Keys);
		
		foreach (var heading in headings)
		{
			csv.WriteField(heading);
		}
		
		csv.NextRecord();
		
		foreach (var item in data)
		{
			foreach (var heading in headings)
			{
				csv.WriteField(item[heading]);
			}
			csv.NextRecord();
		}
		
		Console.WriteLine($"Saving file on path {path}");
	}
}