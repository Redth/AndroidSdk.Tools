using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace AndroidSdk.Tool
{
	[DataContract]
	class DeviceInfo
	{
		public DeviceInfo()
		{
		}

		public DeviceInfo(Adb.AdbDevice adbDevice, Dictionary<string, string> properties, bool printSerial = true)
		{
			Serial = adbDevice?.Serial;
			IsEmulator = adbDevice?.IsEmulator ?? false;
			Properties = properties;
			PrintSerial = printSerial;
		}

		[IgnoreDataMember]
		public bool PrintSerial { get; private set; }

		[DataMember]
		public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

		[DataMember]
		public string Serial { get; private set; }

		[DataMember]
		public bool IsEmulator { get; private set; }

		public override string ToString()
		{
			var s = new StringBuilder();
			if (PrintSerial)
				s.AppendLine(Serial);

			var indent = PrintSerial ? "\t" : string.Empty;

			foreach (var kvp in Properties)
				s.AppendLine($"{indent}{kvp.Key}={kvp.Value}");

			return s.ToString();
		}
	}
}
