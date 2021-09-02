using System;
using System.ComponentModel;
using System.Globalization;

namespace AndroidSdk.Tool
{
	public class OutputFormatTypeConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			=> sourceType == typeof(string);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			var str = value.ToString();
			if (str.Equals("json", StringComparison.OrdinalIgnoreCase))
				return OutputFormat.Json;

			if (str.Equals("xml", StringComparison.OrdinalIgnoreCase))
				return OutputFormat.Xml;


			return OutputFormat.None;
		}
	}
}
