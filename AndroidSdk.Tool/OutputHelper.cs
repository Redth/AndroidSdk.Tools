using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Serialization;

namespace AndroidSdk.Tool
{
	static class OutputHelper
	{
		internal static void Output<T>(IEnumerable<T> items, OutputFormat? format, string[] columns, Func<T, string[]> getRow)
		{
			if ((format ?? OutputFormat.None) == OutputFormat.None)
			{
				OutputTable<T>(items, columns, getRow);
			}
			else
			{
				if (format == OutputFormat.Json)
					AnsiConsole.WriteLine(JsonSerialize<IEnumerable<T>>(items));
				else if (format == OutputFormat.Xml)
					AnsiConsole.WriteLine(XmlSerialize<IEnumerable<T>>(items));
			}
		}

		internal static void OutputTable<T>(IEnumerable<T> items, string[] columns, Func<T, string[]> getRow)
		{
			var table = new Table();

			foreach (var c in columns)
				table.AddColumn(c);

			foreach (var i in items)
			{
				var row = getRow(i);
				table.AddRow(row);
			}

			// Render the table to the console
			AnsiConsole.Render(table);
		}

		internal static void Output<T>(T data, OutputFormat outputFormat)
		{
			var r = string.Empty;
			switch (outputFormat)
			{
				case OutputFormat.None:
					if (data is IEnumerable)
					{
						var enumerator = ((IEnumerable)data).GetEnumerator();
						while (enumerator.MoveNext())
							r += enumerator.Current.ToString() + Environment.NewLine;
					}
					else
					{
						r = data.ToString();
					}
					break;
				case OutputFormat.Json:
					r = JsonSerialize<T>(data);
					break;
				case OutputFormat.Xml:
					r = XmlSerialize<T>(data);
					break;
			}

			Console.Write(r);
		}

		static string JsonSerialize<T>(T obj)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);

			//var s = new DataContractJsonSerializerSettings();
			//s.UseSimpleDictionaryFormat = true;

			//var js = new DataContractJsonSerializer(typeof(T), s);
			//using (var ms = new MemoryStream())
			//{
			//	js.WriteObject(ms, obj);
			//	ms.Position = 0;
			//	using (var sr = new StreamReader(ms))
			//		return sr.ReadToEnd();
			//}
		}

		static string XmlSerialize<T>(T obj)
		{
			var xml = new XmlSerializer(typeof(T));

			using (var textWriter = new StringWriter())
			{
				xml.Serialize(textWriter, obj);
				return textWriter.ToString();
			}
		}
	}
}
