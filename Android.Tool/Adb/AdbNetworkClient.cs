using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Android.Tool.Adb
{
	internal static class AdbNetworkClient
	{
		internal static string GetAvdName(string deviceSerial)
		{
			if (!deviceSerial.StartsWith("emulator-", StringComparison.OrdinalIgnoreCase))
				return null;

			int port = 5554;
			if (!int.TryParse(deviceSerial.Substring(9), out port))
				return null;

			var tcpClient = new System.Net.Sockets.TcpClient("localhost", port);
			var name = string.Empty;
			using (var s = tcpClient.GetStream())
			{

				System.Threading.Thread.Sleep(250);

				foreach (var b in System.Text.Encoding.ASCII.GetBytes("avd name\r\n"))
					s.WriteByte(b);

				System.Threading.Thread.Sleep(250);

				byte[] data = new byte[1024];
				using (var memoryStream = new MemoryStream())
				{
					do
					{
						var len = s.Read(data, 0, data.Length);
						memoryStream.Write(data, 0, len);
					} while (s.DataAvailable);

					var txt = Encoding.ASCII.GetString(memoryStream.ToArray(), 0, (int)memoryStream.Length);

					var m = Regex.Match(txt, "OK(?<name>.*?)OK", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
					name = m?.Groups?["name"]?.Value?.Trim();
				}
			}

			return name;
		}
	}
}
