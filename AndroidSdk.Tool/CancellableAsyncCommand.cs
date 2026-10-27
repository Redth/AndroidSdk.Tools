using Spectre.Console.Cli;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSdk.Tool;

public abstract class CancellableAsyncCommand<TSettings> : AsyncCommand<TSettings>
	where TSettings : CommandSettings
{
	public abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellation);

	public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
	{
		using var cancellationSource = new CancellationTokenSource();

		using var sigInt = OperatingSystem.IsWindows() ? null : PosixSignalRegistration.Create(PosixSignal.SIGINT, onSignal);
		using var sigQuit = OperatingSystem.IsWindows() ? null : PosixSignalRegistration.Create(PosixSignal.SIGQUIT, onSignal);
		using var sigTerm = OperatingSystem.IsWindows() ? null : PosixSignalRegistration.Create(PosixSignal.SIGTERM, onSignal);

		ConsoleCancelEventHandler onConsoleCancel = (sender, args) =>
		{
			args.Cancel = true;
			cancellationSource.Cancel();
		};
		Console.CancelKeyPress += onConsoleCancel;
		try
		{
			return await ExecuteAsync(context, settings, cancellationSource.Token);
		}
		finally
		{
			Console.CancelKeyPress -= onConsoleCancel;
		}

		void onSignal(PosixSignalContext context)
		{
			context.Cancel = true;
			cancellationSource.Cancel();
		}
	}
}