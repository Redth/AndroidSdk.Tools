#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AndroidSdk.Tool;

[DataContract]
public sealed class AvdStartedIdentity
{
	[DataMember(Name = "name")]
	public string Name { get; set; } = "";
	[DataMember(Name = "serial")]
	public string? Serial { get; set; }
	[DataMember(Name = "port")]
	public int? Port { get; set; }
	[DataMember(Name = "standardOutputPath")]
	public string StandardOutputPath { get; set; } = "";
	[DataMember(Name = "standardErrorPath")]
	public string StandardErrorPath { get; set; } = "";
}

// A private owner keeps redirected emulator log streams alive after the launching CLI exits.
// Stop requests go to that live owner, never to a PID or a rediscovered ADB device.
internal static class AvdSession
{
	sealed class Reply
	{
		public AvdStartedIdentity? Identity { get; set; }
		public string? Error { get; set; }
	}

	sealed class StopRequest
	{
		public string? Name { get; set; }
		public string? Serial { get; set; }
		public string? Generation { get; set; }
	}

	static NamedPipeServerStream Server(string name) =>
		new(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

	static NamedPipeClientStream Client(string name) =>
		new(".", name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

	static StreamReader Reader(Stream stream) => new(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
	static StreamWriter Writer(Stream stream) => new(stream, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };

	static async Task SendAsync<T>(StreamWriter writer, T value, CancellationToken token) =>
		await writer.WriteLineAsync(JsonConvert.SerializeObject(value)).WaitAsync(token);

	static async Task<T> ReceiveAsync<T>(StreamReader reader, CancellationToken token)
	{
		var line = await reader.ReadLineAsync().WaitAsync(token);
		if (line == null)
			throw new EndOfStreamException("AVD owner connection closed before acknowledging the operation.");
		return JsonConvert.DeserializeObject<T>(line) ?? throw new InvalidDataException("Invalid AVD owner response.");
	}

	static string Endpoint(string home, string kind, string identity)
	{
		var key = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\n" + home + "\n" + kind + "\n" + identity;
		return "av" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).Substring(0, 32);
	}

	static string SdkHome(string? home) => Path.TrimEndingDirectorySeparator(new Emulator(home).AndroidSdkHome?.FullName ??
		throw new DirectoryNotFoundException("Could not locate the Android SDK. Specify --home."));

	static FileStream LockOwner(string endpoint)
	{
		var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidSdk", "avd-locks");
		Directory.CreateDirectory(directory);
		// Keep the file, and hold its OS lock for the entire lifetime. Unlinking it permits two owners.
		return new FileStream(Path.Combine(directory, endpoint + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
	}

	static ProcessStartInfo HostStartInfo(string pipe)
	{
		var current = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the running CLI host.");
		var isAppHost = string.Equals(Path.GetFileNameWithoutExtension(current), "android", StringComparison.OrdinalIgnoreCase);
		var executable = isAppHost ? current : Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ??
			(string.Equals(Path.GetFileNameWithoutExtension(current), "dotnet", StringComparison.OrdinalIgnoreCase) ? current :
			Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet")));
		var info = new ProcessStartInfo(executable)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		if (!isAppHost)
		{
			var assembly = typeof(AvdSession).Assembly.Location;
			var runtimeConfig = Path.ChangeExtension(assembly, ".runtimeconfig.json");
			var deps = Path.ChangeExtension(assembly, ".deps.json");
			if (!File.Exists(runtimeConfig))
			{
				// Referenced CLI assemblies can run under another application's runtime/dependencies.
				deps = ((string?)AppContext.GetData("APP_CONTEXT_DEPS_FILES"))?.Split(';')
					.FirstOrDefault(p => File.Exists(Path.ChangeExtension(p, ".runtimeconfig.json")))
					?? throw new FileNotFoundException("Cannot locate the current host runtime configuration.");
				runtimeConfig = Path.ChangeExtension(deps, ".runtimeconfig.json");
			}
			info.ArgumentList.Add("exec");
			info.ArgumentList.Add("--runtimeconfig");
			info.ArgumentList.Add(runtimeConfig);
			info.ArgumentList.Add("--depsfile");
			info.ArgumentList.Add(deps);
			info.ArgumentList.Add(assembly);
		}
		info.ArgumentList.Add("--avd-session-host");
		info.ArgumentList.Add(pipe);
		return info;
	}

	internal static async Task<AvdStartedIdentity> StartAsync(AvdStartCommandSettings settings, CancellationToken cancellation)
	{
		settings.Home = SdkHome(settings.Home);
		settings.LogDirectory = Path.GetFullPath(settings.LogDirectory ?? Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidSdk", "avd-logs"));
		var generation = Guid.NewGuid().ToString("N");
		using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		startup.CancelAfter(TimeSpan.FromSeconds(settings.Timeout ?? 300));
		using var pipe = Server("as" + generation);
		using var host = new Process { StartInfo = HostStartInfo("as" + generation) };
		host.Start();
		var detaching = false;
		try
		{
			var connection = pipe.WaitForConnectionAsync(startup.Token);
			await Task.WhenAny(connection, host.WaitForExitAsync(startup.Token));
			if (!connection.IsCompleted)
				throw new InvalidOperationException("AVD owner exited before connecting: " + await host.StandardError.ReadToEndAsync());
			await connection;
			using var reader = Reader(pipe);
			using var writer = Writer(pipe);
			await SendAsync(writer, settings, startup.Token);
			var reply = await ReceiveAsync<Reply>(reader, startup.Token);
			if (reply.Error != null)
				throw new InvalidOperationException(reply.Error);
			var identity = reply.Identity ?? throw new InvalidDataException("AVD owner did not return an identity.");
			detaching = !settings.WaitForExit;
			await SendAsync(writer, settings.WaitForExit ? "attach" : "detach", startup.Token);
			var acknowledgement = await ReceiveAsync<Reply>(reader, startup.Token);
			if (acknowledgement.Error != null)
				throw new InvalidOperationException(acknowledgement.Error);
			if (settings.WaitForExit)
			{
				var completed = await ReceiveAsync<Reply>(reader, cancellation);
				if (completed.Error != null)
					throw new InvalidOperationException(completed.Error);
				using var exit = new CancellationTokenSource(TimeSpan.FromSeconds(10));
				await host.WaitForExitAsync(exit.Token);
				if (host.ExitCode != 0)
					throw new InvalidOperationException($"AVD owner exited with code {host.ExitCode}.");
			}
			return identity;
		}
		catch (Exception failure)
		{
			// EOF cancels startup/attached ownership. Never kill the owner and orphan its emulator.
			pipe.Dispose();
			try
			{
				using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(20));
				var exited = host.WaitForExitAsync(cleanup.Token);
				if (detaching && await Task.WhenAny(exited, Task.Delay(1000, cleanup.Token)) != exited)
					await StopAsync(settings.Home, settings.Name, null, generation, cleanup.Token);
				await exited;
			}
			catch (Exception cleanupFailure)
			{
				throw new AggregateException("AVD start failed and exact-process cleanup could not be confirmed.", failure, cleanupFailure);
			}
			var diagnostics = await host.StandardError.ReadToEndAsync();
			throw new InvalidOperationException($"AVD start failed. Emulator logs: {settings.LogDirectory}\n{diagnostics}", failure);
		}
	}

	internal static async Task StopAsync(string? home, string? name, string? serial, string? generation, CancellationToken token)
	{
		var sdk = SdkHome(home);
		var endpoint = Endpoint(sdk, name != null ? "name" : "serial", name ?? serial!);
		using var pipe = Client(endpoint);
		await pipe.ConnectAsync(token);
		using var reader = Reader(pipe);
		using var writer = Writer(pipe);
		await SendAsync(writer, new StopRequest { Name = name, Serial = serial, Generation = generation }, token);
		var response = await ReceiveAsync<Reply>(reader, token);
		if (response.Error != null)
			throw new InvalidOperationException(response.Error);
	}

	static async Task ServeStopAsync(NamedPipeServerStream pipe, AvdStartedIdentity identity, string generation,
		CancellationTokenSource requested, Task<Reply> completion, CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			await pipe.WaitForConnectionAsync(token);
			try
			{
				using var reader = Reader(pipe);
				using var writer = Writer(pipe);
				using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
				requestTimeout.CancelAfter(TimeSpan.FromSeconds(5));
				var request = await ReceiveAsync<StopRequest>(reader, requestTimeout.Token);
				if ((request.Name == null && request.Serial == null) ||
					(request.Name != null && request.Name != identity.Name) ||
					(request.Serial != null && request.Serial != identity.Serial) ||
					(request.Generation != null && request.Generation != generation))
				{
					await SendAsync(writer, new Reply { Error = "The requested identity does not match this live AVD owner." }, requestTimeout.Token);
				}
				else
				{
					requested.Cancel();
					await SendAsync(writer, await completion.WaitAsync(token), token);
					return;
				}
			}
			catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is JsonException)
			{
				if (requested.IsCancellationRequested)
					throw;
				// A rejected/disconnected client must not stop the owner or block subsequent stop requests.
				Console.Error.WriteLine("Rejected AVD stop connection: " + ex.Message);
			}
			finally
			{
				if (pipe.IsConnected)
					pipe.Disconnect();
			}
		}
	}

	static async Task<int> ReportPortAsync(TcpListener listener, CancellationToken token)
	{
		using var client = await listener.AcceptTcpClientAsync(token);
		using var stream = client.GetStream();
		var buffer = new byte[32];
		var count = 0;
		while (count < buffer.Length)
		{
			var read = await stream.ReadAsync(buffer.AsMemory(count), token);
			if (read == 0)
				break;
			count += read;
			if (Array.IndexOf(buffer, (byte)'\n', 0, count) >= 0)
				break;
		}
		if (!int.TryParse(Encoding.ASCII.GetString(buffer, 0, count).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
			port < 5554 || port > 5682 || port % 2 != 0)
			throw new InvalidDataException("Emulator reported an invalid console port.");
		return port;
	}

	static async Task PrepareAsync(Emulator.AndroidEmulatorProcess process, AvdStartCommandSettings settings, Task<int>? portReport,
		AvdStartedIdentity identity, CancellationToken token)
	{
		if (portReport != null)
		{
			while (!portReport.IsCompleted)
			{
				token.ThrowIfCancellationRequested();
				process.Runner.ThrowIfLogFailed();
				if (process.HasExited)
					throw new InvalidOperationException("Emulator exited before reporting its console port.");
				await Task.WhenAny(portReport, Task.Delay(100, token));
			}
			var port = await portReport;
			if (settings.Port.HasValue && settings.Port.Value != port)
				throw new InvalidDataException("Emulator reported a different port than --port.");
			identity.Port = port;
			var serial = "emulator-" + port.ToString(CultureInfo.InvariantCulture);
			await Task.Run(() =>
			{
				if (!process.WaitForBootComplete(token, serial))
					throw new InvalidOperationException("Emulator did not finish booting.");
				if (settings.DisableAnimations)
					process.DisableAnimations(token);
				if (settings.CpuThreshold.HasValue &&
					!process.WaitForCpuLoadBelow(settings.CpuThreshold.Value, TimeSpan.Zero, TimeSpan.FromSeconds(10), token))
					throw new InvalidOperationException("Emulator CPU load did not settle.");
				if (settings.ResponseThreshold.HasValue &&
					!process.WaitForStableResponses(3, settings.ResponseThreshold.Value, TimeSpan.Zero, token))
					throw new InvalidOperationException("Emulator responses did not stabilize.");
				token.ThrowIfCancellationRequested();
				process.Runner.ThrowIfLogFailed();
				if (process.HasExited)
					throw new InvalidOperationException("Emulator exited during readiness.");
			}, token);
			identity.Serial = serial;
		}
		token.ThrowIfCancellationRequested();
		process.Runner.ThrowIfLogFailed();
	}

	static Emulator.EmulatorStartOptions Options(AvdStartCommandSettings settings, TcpListener? report) => new()
	{
		WipeData = settings.WipeData, NoSnapshot = settings.NoSnapshot, Acceleration = settings.Acceleration,
		Engine = settings.Engine, NoWindow = settings.NoWindow, NoAudio = settings.NoAudio,
		NoJni = settings.NoJni, NoBootAnim = settings.NoBootAnimation, Gpu = settings.Gpu,
		Port = settings.Port, CameraBack = settings.CameraBack, CameraFront = settings.CameraFront,
		NoSnapshotLoad = settings.NoSnapshotLoad, NoSnapshotSave = settings.NoSnapshotSave,
		Verbose = settings.Verbose, ShowKernel = settings.ShowKernel, Screen = settings.ScreenMode,
		MemoryMegabytes = (int?)settings.Memory, Cores = settings.Cores,
		PartitionSizeMegabytes = (int?)settings.PartitionSize, CacheSizeMegabytes = (int?)settings.CacheSize,
		GrpcPort = (int?)settings.GrpcPort, GrpcUseJwt = settings.GrpcUseJwt,
		ExtraArgs = report == null ? null : ["-report-console", $"tcp:{((IPEndPoint)report.LocalEndpoint).Port},max=5"]
	};

	static Exception? Cleanup(Emulator.AndroidEmulatorProcess? process, AvdProcessContainment? containment, bool kill, Exception? failure)
	{
		void Attempt(Action action)
		{
			try { action(); }
			catch (Exception ex)
			{
				failure = failure == null ? ex : new AggregateException(failure, ex);
			}
		}
		if (kill && process != null)
			Attempt(process.Runner.KillAndWait);
		if (containment != null)
			Attempt(containment.StopAndWait);
		if (process != null)
		{
			Attempt(() =>
			{
				if (!process.HasExited)
					throw new IOException("The exact emulator process remains alive after cleanup.");
				if (kill)
					process.Runner.WaitForExit();
				else
					process.WaitForExit();
			});
		}
		return failure;
	}

	internal static async Task<int> RunHostAsync(string startupPipe)
	{
		using var lifetime = new CancellationTokenSource();
		using var sigInt = OperatingSystem.IsWindows() ? null : PosixSignalRegistration.Create(PosixSignal.SIGINT, Cancel);
		using var sigQuit = OperatingSystem.IsWindows() ? null : PosixSignalRegistration.Create(PosixSignal.SIGQUIT, Cancel);
		using var sigTerm = OperatingSystem.IsWindows() ? null : PosixSignalRegistration.Create(PosixSignal.SIGTERM, Cancel);
		void Cancel(PosixSignalContext context) { context.Cancel = true; lifetime.Cancel(); }

		using var bootstrap = Client(startupPipe);
		using var connect = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		Emulator.AndroidEmulatorProcess? process = null;
		AvdProcessContainment? containment = null;
		Exception? failure = null;
		var cleaned = false;
		Task<string>? control = null;
		Task? nameServer = null;
		Task? serialServer = null;
		NamedPipeServerStream? byName = null;
		NamedPipeServerStream? bySerial = null;
		FileStream? nameLock = null;
		FileStream? serialLock = null;
		TcpListener? report = null;
		using var stop = new CancellationTokenSource();
		var stopTask = Task.Delay(Timeout.Infinite, stop.Token);
		var completion = new TaskCompletionSource<Reply>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var listeners = new CancellationTokenSource();
		var attached = false;
		var handedOff = false;
		StreamWriter? writer = null;
		Task? parentMonitor = null;
		try
		{
			await bootstrap.ConnectAsync(connect.Token);
			using var reader = Reader(bootstrap);
			writer = Writer(bootstrap);
			var settings = await ReceiveAsync<AvdStartCommandSettings>(reader, connect.Token);
			containment = new AvdProcessContainment();
			using var startup = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, stop.Token);
			startup.CancelAfter(TimeSpan.FromSeconds(settings.Timeout ?? 300));
			var identity = new AvdStartedIdentity { Name = settings.Name };
			var generation = startupPipe.Substring(2);
			var nameEndpoint = Endpoint(settings.Home, "name", settings.Name);
			nameLock = LockOwner(nameEndpoint);
			byName = Server(nameEndpoint);
			nameServer = ServeStopAsync(byName, identity, generation, stop, completion.Task, listeners.Token);
			control = ReceiveAsync<string>(reader, lifetime.Token);
			parentMonitor = CancelOnDisconnectAsync(control, lifetime);
			Directory.CreateDirectory(settings.LogDirectory);
			var logId = Guid.NewGuid().ToString("N");
			identity.StandardOutputPath = Path.Combine(settings.LogDirectory, logId + ".stdout.log");
			identity.StandardErrorPath = Path.Combine(settings.LogDirectory, logId + ".stderr.log");
			Task<int>? portReport = null;
			using var reporting = CancellationTokenSource.CreateLinkedTokenSource(startup.Token);
			if (settings.WaitForBoot)
			{
				report = new TcpListener(IPAddress.Loopback, 0);
				report.Start();
				portReport = ReportPortAsync(report, reporting.Token);
			}
			startup.Token.ThrowIfCancellationRequested();
			process = new Emulator(settings.Home).Start(settings.Name, Options(settings, report),
				identity.StandardOutputPath, identity.StandardErrorPath);
			try
			{
				await PrepareAsync(process, settings, portReport, identity, startup.Token);
			}
			finally
			{
				reporting.Cancel();
				report?.Stop();
				if (portReport != null)
				{
					try { await portReport; }
					catch (Exception ex) when (ex is OperationCanceledException || ex is SocketException || ex is ObjectDisposedException)
					{
						if (!reporting.IsCancellationRequested)
							throw;
					}
				}
			}
			if (identity.Serial != null)
			{
				var serialEndpoint = Endpoint(settings.Home, "serial", identity.Serial);
				serialLock = LockOwner(serialEndpoint);
				bySerial = Server(serialEndpoint);
				serialServer = ServeStopAsync(bySerial, identity, generation, stop, completion.Task, listeners.Token);
			}
			await SendAsync(writer, new Reply { Identity = identity }, startup.Token);
			var mode = await control.WaitAsync(startup.Token);
			if (mode != "detach" && mode != "attach")
				throw new InvalidDataException("Invalid AVD startup acknowledgement.");
			attached = mode == "attach";
			await SendAsync(writer, new Reply(), startup.Token);
			startup.CancelAfter(Timeout.InfiniteTimeSpan);
			handedOff = true;
			// Attached callers retain cancellation ownership; detached callers may close their pipe.
			if (attached)
				parentMonitor = CancelOnDisconnectAsync(ReceiveAsync<string>(reader, lifetime.Token), lifetime);
			while (!stop.IsCancellationRequested)
			{
				lifetime.Token.ThrowIfCancellationRequested();
				if (nameServer.IsFaulted)
					await nameServer;
				if (serialServer?.IsFaulted == true)
					await serialServer;
				if (!cleaned)
				{
					try
					{
						process.Runner.ThrowIfLogFailed();
						if (process.HasExited)
						{
							failure = Cleanup(process, containment, false, null);
							cleaned = true;
						}
					}
					catch (Exception ex)
					{
						failure = Cleanup(process, containment, true, ex);
						cleaned = true;
					}
				}
				if (cleaned && attached)
					break;
				await Task.WhenAny(stopTask, Task.Delay(100, lifetime.Token));
			}
		}
		catch (Exception ex)
		{
			failure = process?.LastReadinessError == null ? ex : new AggregateException(ex, process.LastReadinessError);
		}
		finally
		{
			report?.Stop();
			if (!cleaned)
				failure = Cleanup(process, containment, true, failure);
			var reply = new Reply { Error = failure == null ? null : Describe(failure) };
			completion.TrySetResult(reply);
			if (writer != null && (!handedOff || attached))
			{
				try
				{
					using var responseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
					await SendAsync(writer, reply, responseTimeout.Token);
				}
				catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is OperationCanceledException)
				{
					failure = failure == null ? ex : new AggregateException(failure, ex);
				}
			}
			if (stop.IsCancellationRequested)
			{
				try
				{
					using var responseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
					await Task.WhenAny(nameServer ?? Task.CompletedTask, serialServer ?? Task.Delay(Timeout.Infinite)).WaitAsync(responseTimeout.Token);
				}
				catch (Exception ex) { failure = failure == null ? ex : new AggregateException(failure, ex); }
			}
			listeners.Cancel();
			byName?.Dispose();
			bySerial?.Dispose();
			serialLock?.Dispose();
			nameLock?.Dispose();
			writer?.Dispose();
			bootstrap.Dispose();
			if (parentMonitor != null)
				await parentMonitor;
			process?.Runner.NativeProcess.Dispose();
			containment?.Dispose();
		}
		if (failure != null)
		{
			Console.Error.WriteLine(failure);
			return 1;
		}
		return 0;
	}

	static string Describe(Exception failure)
	{
		var text = failure.ToString();
		if (failure is SdkToolFailedExitException sdk)
			text += "\n" + string.Join("\n", sdk.StdErr) + "\n" + string.Join("\n", sdk.StdOut);
		if (failure is AggregateException aggregate)
			foreach (var inner in aggregate.InnerExceptions)
				text += "\n" + Describe(inner);
		return text;
	}

	static async Task CancelOnDisconnectAsync(Task<string> control, CancellationTokenSource startup)
	{
		try { await control; }
		catch (Exception)
		{
			startup.Cancel();
		}
	}

}
