using AndroidSdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static AndroidSdk.Adb;

namespace AndroidSdk;

#if NET6_0_OR_GREATER
public class AdbdClient
{
	public const int DefaultAdbdPort = 5037;

	public AdbdClient(DirectoryInfo androidSdkHome, string? host = null, int? port = null, ILogger? logger = default)
	{
		Host = host ?? IPAddress.Loopback.ToString();
		Port = port ?? 5037;
		Logger = logger ?? NullLogger.Instance;

		adb = new Adb(androidSdkHome);
	}

	public string? Transport { get; private set; }
	protected readonly ILogger Logger;

	public readonly string Host;
	public readonly int Port;

	public string? AndroidSdkHome => adb.AndroidSdkHome?.FullName;

	TcpClient tcpClient = new TcpClient();
	readonly Adb adb;
	NetworkStream? stream;

	const int defaultWaitBackoff = 1000;

	int waitBackoff = defaultWaitBackoff;
	int connectionAttempts = 1;

	async Task EnsureConnectedAsync(CancellationToken cancellationToken)
	{
		while ((!tcpClient.Connected || stream is null || !stream.CanWrite) && !cancellationToken.IsCancellationRequested)
		{
			try
			{
				// If we failed already, maybe try killing and restarting adb
				if (waitBackoff > defaultWaitBackoff)
				{
					Logger.LogTrace("TcpClient not connected after {connectAttempts}, waiting {waitBackoff}ms...", connectionAttempts, waitBackoff);

					if (!cancellationToken.IsCancellationRequested)
					{
						Logger.LogTrace("Stopping adb server...");
						KillServer();
					}

					if (!cancellationToken.IsCancellationRequested)
					{
						Logger.LogTrace("Restarting adb server...");
						StartServer();
					}
				}
			}
			catch { }

			try
			{
				Logger.LogTrace("Connecting: {Host}:{Port} (attempt {connectAttempts})...", Host, Port, connectionAttempts);
				Transport = null;

				tcpClient = new TcpClient();
				await tcpClient.ConnectAsync(Host, Port, cancellationToken).ConfigureAwait(false);

				stream = tcpClient.GetStream();

				// Reset wait backoff since we have a connection now
				waitBackoff = defaultWaitBackoff;
				connectionAttempts = 1;


				Logger.LogTrace("Connected: {Host}:{Port}.", Host, Port);
				return;
			}
			catch (SocketException ex)
			{
				Logger.LogWarning("TcpClient SocketException: {ex}", ex);
			}

			if (!tcpClient.Connected)
			{
				// Exponential backoff
				waitBackoff += (int)(waitBackoff * 1.5);
				connectionAttempts++;

				Logger.LogTrace("Not connected, waiting {waitBackoff}ms before retry...", waitBackoff);

				// Wait before retrying
				await Task.Delay(waitBackoff, cancellationToken).ConfigureAwait(false);
			}
		}
	}

	public void KillServer()
	{
		//adb kill-server
		adb.KillServer();
	}

	public void StartServer()
	{
		//adb start-server
		adb.StartServer();
	}

	public void Disconnect()
	{
		try { stream?.Dispose(); }
		catch { }
		finally { stream = null; }

		try { tcpClient?.Close(); }
		catch { }

		Transport = null;
		waitBackoff = defaultWaitBackoff;
		connectionAttempts = 1;
	}

	async Task SendCommandAsync(string command, CancellationToken cancellationToken)
	{
		await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

		var c = $"{command.Length.ToString("X4")}{command}";

		Logger?.LogTrace("TX: {c}", c);

		var data = System.Text.Encoding.ASCII.GetBytes(c);

		try
		{
			if (stream is not null)
			{
				await stream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
				await stream.FlushAsync().ConfigureAwait(false);
			}
		}
		catch (OperationCanceledException)
		{
			throw new Exception($"Command '{command}' failed: Operation Canceled");
		}

		if (!await WaitForOkayAsync(cancellationToken).ConfigureAwait(false))
		{
			var reason = await ReadNextReplyAsync(cancellationToken).ConfigureAwait(false);

			Disconnect();

			throw new Exception($"Command '{command}' failed: {reason}");
		}
	}

	async Task<bool> WaitForOkayAsync(CancellationToken cancellationToken)
	{
		var buffer = new byte[4];
		var result = string.Empty;

		while (true)
		{
			int read;

			try
			{
				if (stream is not null)
					read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
				else
					read = -1;
			}
			catch (OperationCanceledException)
			{
				read = -1;
			}
			if (read <= 0)
				break;

			result += System.Text.Encoding.ASCII.GetString(buffer, 0, read);

			Logger?.LogTrace("RX: {result}", result);

			if (result.Length >= 4)
			{
				return result.Equals("OKAY", StringComparison.OrdinalIgnoreCase);
			}
		}

		Logger?.LogTrace("No OKAY returned...");
		Disconnect();
		return false;
	}

	string currentMessage = string.Empty;
	int currentMessageLength = -1;

	bool TryGetNextCompletedMessage(out string? message)
	{
		if (currentMessage.Length >= currentMessageLength + 4)
		{
			message = currentMessage.Substring(4, currentMessageLength);

			currentMessage = currentMessage.Remove(0, currentMessageLength + 4);
			currentMessageLength = -1;

			return true;
		}

		message = null;
		return false;
	}

	async Task<string?> ReadReplyBodyAsync(CancellationToken cancellationToken = default)
	{
		var reply = string.Empty;
		var buffer = new byte[1024];

		while (true)
		{
			if (tcpClient is null || !tcpClient.Connected || stream is null || !stream.CanRead)
				return reply;

			int read;

			try
			{
				if (stream is not null)
					read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
				else
					read = -1;
			}
			catch (OperationCanceledException) { read = -1; }

			if (read <= 0)
				break;

			var str = System.Text.Encoding.ASCII.GetString(buffer, 0, read);

			Logger?.LogTrace("RX: {str}", str);

			reply += str;
		}

		return reply;
	}

	async Task<string?> ReadNextReplyAsync(CancellationToken cancellationToken = default)
	{
		if (TryGetNextCompletedMessage(out var m))
			return m;

		var buffer = new byte[1024];

		while (true)
		{
			if (tcpClient is null || !tcpClient.Connected || stream is null || !stream.CanRead)
				return currentMessage;

			int read;

			try
			{
				read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException) { read = -1; }

			if (read <= 0)
				break;

			var str = System.Text.Encoding.ASCII.GetString(buffer, 0, read);

			Logger?.LogTrace("RX: {str}", str);

			currentMessage += str;

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

		return currentMessage;
	}


	public async Task<int> GetHostVersionAsync(CancellationToken cancellationToken = default)
	{
		await SendCommandAsync("host:version", cancellationToken).ConfigureAwait(false);
		var reply = await ReadNextReplyAsync(cancellationToken).ConfigureAwait(false);

		return Int32.Parse(reply ?? "-1", System.Globalization.NumberStyles.HexNumber);
	}


	public async Task<bool> TransportAsync(string serial, CancellationToken cancellationToken = default)
	{
		// Don't switch if we already are on this transport
		if (serial.Equals(Transport, StringComparison.OrdinalIgnoreCase))
			return true;

		try
		{
			await SendCommandAsync($"host:transport:{serial}", cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Logger.LogDebug("Switching Transport failed: {ex}", ex);
			return false;
		}
		return true;
	}

	public async Task<List<AdbDevice>> ListDevicesAsync(CancellationToken cancellationToken = default)
	{
		var devices = new List<AdbDevice>();

		try
		{
			await SendCommandAsync("host:devices-l", cancellationToken).ConfigureAwait(false);

			var str = await ReadNextReplyAsync(cancellationToken).ConfigureAwait(false);

			var lines = str?.Split("\n") ?? new string[0];

			foreach (var line in lines)
			{
				var parts = Regex.Split(line.Trim(), "\\s+");

				var d = new AdbDevice(parts[0].Trim());

				if (parts.Length > 1 && (parts[1]?.ToLowerInvariant() ?? "offline") == "offline")
					continue;

				if (parts.Length > 2)
				{
					foreach (var part in parts.Skip(2))
					{
						var bits = part.Split(new[] { ':' }, 2);
						if (bits == null || bits.Length != 2)
							continue;

						switch (bits[0].ToLower())
						{
							case "usb":
								d.Usb = bits[1];
								break;
							case "product":
								d.Product = bits[1];
								break;
							case "model":
								d.Model = bits[1];
								break;
							case "device":
								d.Device = bits[1];
								break;
						}
					}
				}

				if (!string.IsNullOrEmpty(d?.Serial))
					devices.Add(d);
			}
		}
		catch
		{
		}

		// Expect a disconnect from the server here
		Disconnect();

		return devices;
	}


	public async Task<string?> ShellAsync(string serial, string command, string[]? args = null, CancellationToken cancellationToken = default)
	{
		string? result = null;

		try
		{
			await TransportAsync(serial, cancellationToken).ConfigureAwait(false);

			var argStr = string.Empty;

			if (args is not null && args.Length > 0)
				argStr = string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));

			await SendCommandAsync($"shell:{command} {argStr}".Trim(), cancellationToken).ConfigureAwait(false);

			result = await ReadReplyBodyAsync(cancellationToken).ConfigureAwait(false);
		}
		catch
		{
		}

		Disconnect();

		return result;
	}

	public async Task<string?> GetPropAsync(string serial, string property, CancellationToken cancellationToken = default)
	{
		var v = await ShellAsync(serial, "getprop", new[] { property }, cancellationToken).ConfigureAwait(false);
		return v?.Trim();
	}

	Lazy<Regex> rxShellProp = new Lazy<Regex>(() => new Regex(@"\[(?<key>.*?)\]:\s+?\[(?<value>.*?)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline));

	public async Task<IReadOnlyDictionary<string, string>> GetAllPropsAsync(string serial, CancellationToken cancellationToken = default)
	{
		var v = await ShellAsync(serial, "getprop", null, cancellationToken).ConfigureAwait(false);

		var props = new Dictionary<string, string>();

		if (string.IsNullOrEmpty(v))
			return props;

		using var r = new StringReader(v);

		while (true)
		{
			var line = await r.ReadLineAsync().ConfigureAwait(false);

			if (string.IsNullOrEmpty(line))
				break;

			var match = rxShellProp.Value.Match(line);

			if (match.Groups.TryGetValue("key", out var keyGrp) && keyGrp.Success
				&& match.Groups.TryGetValue("value", out var valGrp) && valGrp.Success)
			{
				if (!string.IsNullOrWhiteSpace(keyGrp.Value) && !string.IsNullOrEmpty(valGrp.Value))
					props[keyGrp.Value] = valGrp.Value;
			}

		}

		return props;
	}

	public async Task<IReadOnlyList<string>> GetFeaturesAsync(string serial, CancellationToken cancellationToken = default)
	{
		var v = await ShellAsync(serial, "pm", new[] { "list", "features" }, cancellationToken).ConfigureAwait(false);

		var features = new List<string>();

		if (string.IsNullOrEmpty(v))
			return features;

		using var r = new StringReader(v);

		while (true)
		{
			var line = await r.ReadLineAsync().ConfigureAwait(false);

			if (string.IsNullOrEmpty(line))
				break;

			if (line.StartsWith("feature:", StringComparison.OrdinalIgnoreCase))
			{
				var f = line.Substring(8);
				if (!string.IsNullOrEmpty(f))
					features.Add(f);
			}
		}

		return features.AsReadOnly();
	}


	Regex rxWhitespace = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);


	public async Task WatchDevicesAsync(CancellationToken cancellationToken, Func<DeviceInfo, Task> handle)
	{
		await SendCommandAsync("host:track-devices-l", cancellationToken).ConfigureAwait(false);

		while (true)
		{
			var reply = await ReadNextReplyAsync(cancellationToken).ConfigureAwait(false);

			if (cancellationToken.IsCancellationRequested)
				break;

			var parts = rxWhitespace.Split(reply ?? string.Empty);

			if (parts.Length > 1)
			{
				string serial = parts[0];
				DeviceState state = ParseState(parts[1]);

				var extra = new Dictionary<string, string>();

				for (int i = 2; i < parts.Length; i++)
				{
					var kvp = parts[i].Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

					if (kvp.Length > 1)
					{
						extra[kvp[0].Trim().ToLowerInvariant()] = kvp[1].Trim();
					}
				}

				if (!extra.TryGetValue("transport_id", out var tidstr) || !int.TryParse(tidstr, out var transportId))
					transportId = -1;

				extra.TryGetValue("product", out var product);
				extra.TryGetValue("model", out var model);
				extra.TryGetValue("device", out var device);

				var d = new DeviceInfo(serial, state, product, model, device, transportId);

				handle?.Invoke(d);
			}
		}
	}

	public DeviceState ParseState(string? state)
		=> state switch
		{
			"offline" => DeviceState.Offline,
			"authorizing" => DeviceState.Authorizing,
			"device" => DeviceState.Device,
			_ => DeviceState.Offline
		};


	public class DeviceInfo
	{
		public DeviceInfo(string serial, DeviceState state, string? product, string? model, string? device, int? transportId)
		{
			Serial = serial;
			State = state;
			Product = product;
			Model = model;
			Device = device;
			TransportId = transportId;
		}

		public readonly string Serial;
		public readonly DeviceState State;
		public readonly string? Product;
		public readonly string? Model;
		public readonly string? Device;
		public readonly int? TransportId;
	}


	public enum DeviceState
	{
		Offline,
		Authorizing,
		Device
	}

	public class ShellProperties
	{
		public const string ProductName = "ro.product.name";
		public const string ProductModel = "ro.product.model";
		public const string ProductBrand = "ro.product.brand";
		public const string ProductManufacturer = "ro.product.manufacturer";
		public const string ProductBuildVersionSdk = "ro.product.build.version.sdk";
		public const string ProductBuildVersionRelease = "ro.product.build.version.release";
		public const string ProductCpuAbi = "ro.product.cpu.abi";

		public const string BuildVersionSdk = "ro.build.version.sdk";
		public const string BuildVersionRelease = "ro.build.version.release";

		public const string AvdName = "ro.boot.qemu.avd_name";
		public const string AvdName2 = "ro.kernel.qemu.avd_name";

		public const string BootComplete = "dev.bootcomplete";
		public const string BootComplete2 = "vendor.qemu.dev.bootcomplete";
	}

	public class AvdProperties
	{
		public const string HwCpuArch = "hw.cpu.arch";
		public const string HwDeviceManufacturer = "hw.device.manufacturer";
		public const string HwDeviceName = "hw.device.name";
	}
}

#endif