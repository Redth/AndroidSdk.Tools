using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AndroidSdk
{
	public class AdbdClient : IDisposable
	{
		public const string DefaultAdbdHost = "127.0.01";
		public const int DefaultAdbdPort = 5037;

		readonly TcpClient tcpClient = new TcpClient();
		StreamReader streamReader;
		StreamWriter streamWriter;

		public async Task ConnectAsync(string host = DefaultAdbdHost, int port = DefaultAdbdPort)
		{
			await tcpClient.ConnectAsync(host, port).ConfigureAwait(false);

			var stream = tcpClient.GetStream();

			streamReader = new StreamReader(stream, System.Text.Encoding.ASCII, false);
			streamWriter = new StreamWriter(stream, System.Text.Encoding.ASCII);
		}

		public void Disconnect()
		{
			try { streamWriter?.Dispose(); }
			catch { }
			finally { streamWriter = null; }

			try { streamReader?.Dispose(); }
			catch { }
			finally { streamReader = null; }

			try { tcpClient?.Close(); }
			catch { }
		}

		Task SendCommandAsync(string command)
			=> streamWriter.WriteAsync($"{command.Length.ToString("X4")}{command}");

		string currentMessage = string.Empty;
		int currentMessageLength = -1;

		bool TryGetNextCompletedMessage(out string message)
		{
			if (currentMessage.Length >= currentMessageLength + 4)
			{
				message = currentMessage.Substring(4, currentMessageLength + 4);

				currentMessage = currentMessage.Remove(0, currentMessageLength + 4);
				currentMessageLength = -1;

				return true;
			}

			message = null;
			return false;
		}

		async Task<string> ReadNextReplyAsync()
		{
			if (TryGetNextCompletedMessage(out var m))
				return m;

			var buffer = new char[1024];

			while (true)
			{
				var read = await streamReader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

				if (read <= 0)
					break;

				currentMessage += new string(buffer, 0, read);

				// If we haven't figured out the message length from the first 4 chars
				// and we have received at least 4 chars, we can figure it out
				// the first 4 chars are hex value of the message length to follow
				if (currentMessageLength < 0 && currentMessage.Length >= 4)
				{
					currentMessageLength = Int32.Parse(currentMessage.Substring(0, 4), System.Globalization.NumberStyles.HexNumber);
				}

				if (TryGetNextCompletedMessage(out var m2))
					return m2;
			}

			return null;
		}


		public async Task<string> GetHostVersionAsync()
		{
			var command = "host:version";
			await SendCommandAsync(command).ConfigureAwait(false);
			var reply = await ReadNextReplyAsync().ConfigureAwait(false);

			return reply;
		}

		public void WatchDevices(Action<string> handle)
		{
			var command = "host:track-devices";

		}
	}
}

