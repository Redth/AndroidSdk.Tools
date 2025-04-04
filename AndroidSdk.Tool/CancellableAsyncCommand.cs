using Spectre.Console.Cli;
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

		using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, onSignal);
		using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, onSignal);
		using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, onSignal);

		var cancellable = ExecuteAsync(context, settings, cancellationSource.Token);
		return await cancellable;

		void onSignal(PosixSignalContext context)
		{
			context.Cancel = true;
			cancellationSource.Cancel();
		}
	}
}