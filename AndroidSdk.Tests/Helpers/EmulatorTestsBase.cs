#nullable enable
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

public abstract class EmulatorTestsBase(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
	: AndroidSdkManagerTestsBase(outputHelper, fixture)
{
	protected static Emulator.EmulatorStartOptions CreateHeadlessOptions(uint? port = null, int? memoryMegabytes = null, int? partitionSizeMegabytes = null)
		=> new()
		{
			Port = port,
			NoWindow = IsCI,
			Gpu = "swiftshader_indirect",
			NoSnapshot = true,
			NoAudio = true,
			NoBootAnim = true,
			MemoryMegabytes = memoryMegabytes,
			PartitionSizeMegabytes = partitionSizeMegabytes,
		};

	protected static void LogSdkToolException(IMessageSink sink, string action, SdkToolFailedExitException ex)
	{
		sink.OnMessage(new DiagnosticMessage($"{action} failed. ExitCode={ex.ExitCode} Message={ex.Message}"));

		if (ex.StdErr?.Length > 0)
		{
			sink.OnMessage(new DiagnosticMessage($"{action} stderr:"));
			foreach (var line in ex.StdErr)
				sink.OnMessage(new DiagnosticMessage(line));
		}

		if (ex.StdOut?.Length > 0)
		{
			sink.OnMessage(new DiagnosticMessage($"{action} stdout:"));
			foreach (var line in ex.StdOut)
				sink.OnMessage(new DiagnosticMessage(line));
		}
	}
}
