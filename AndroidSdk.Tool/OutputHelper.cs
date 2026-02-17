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
					Console.WriteLine(JsonSerialize<IEnumerable<T>>(items));
				else if (format == OutputFormat.JsonPretty)
					Console.WriteLine(JsonSerialize<IEnumerable<T>>(items, indented: true));
				else if (format == OutputFormat.Xml)
					Console.WriteLine(XmlSerialize<IEnumerable<T>>(items));
			}
		}

		internal static void Output<T>(T item, OutputFormat? format, string[] properties, Func<T, string[]> getValues)
		{
			if ((format ?? OutputFormat.None) == OutputFormat.None)
			{
				OutputObject<T>(item, properties, getValues);
			}
			else
			{
				if (format == OutputFormat.Json)
					Console.WriteLine(JsonSerialize<T>(item));
				else if (format == OutputFormat.JsonPretty)
					Console.WriteLine(JsonSerialize<T>(item, indented: true));
				else if (format == OutputFormat.Xml)
					Console.WriteLine(XmlSerialize<T>(item));
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
			AnsiConsole.Write(table);
		}

		internal static void OutputObject<T>(T item, string[] properties, Func<T, string[]> getValues)
		{
			var table = new Table();
			var values = getValues(item);

			table.AddColumn("Property");
			table.AddColumn("Value");

			for (int i = 0; i < properties.Length; i++)
			{
				var name = properties[i];
				var val = values[i] ?? "";

				table.AddRow(name, val);
			}

			// Render the table to the console
			AnsiConsole.Write(table);
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
				case OutputFormat.JsonPretty:
					r = JsonSerialize<T>(data, indented: true);
					break;
				case OutputFormat.Xml:
					r = XmlSerialize<T>(data);
					break;
			}

			Console.Write(r);
		}

		static string JsonSerialize<T>(T obj, bool indented = false)
		{
			var formatting = indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
			return Newtonsoft.Json.JsonConvert.SerializeObject(obj, formatting);

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
