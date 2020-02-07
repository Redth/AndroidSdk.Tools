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
			var s = new DataContractJsonSerializerSettings();
			s.UseSimpleDictionaryFormat = true;

			var js = new DataContractJsonSerializer(typeof(T), s);
			using (var ms = new MemoryStream())
			{
				js.WriteObject(ms, obj);
				ms.Position = 0;

				using (var sr = new StreamReader(ms))
					return sr.ReadToEnd();
			}
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
