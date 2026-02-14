using System.Threading;

namespace AndroidSdk;

internal class JavaProcessRunner : ProcessRunner
{
	public JavaProcessRunner(JdkInfo jdk, JavaProcessArgumentBuilder builder)
		: base(jdk.Java, builder)
	{
	}

	public JavaProcessRunner(JdkInfo jdk, JavaProcessArgumentBuilder builder, CancellationToken cancelToken, bool redirectStandardInput = false)
		: base(jdk.Java, builder, cancelToken, redirectStandardInput)
	{
	}

	public JavaProcessRunner(JdkInfo jdk, JavaProcessArgumentBuilder builder, CancellationToken cancelToken, bool redirectStandardInput, Action<string> outputHandler, Action<string> errorHandler)
		: base(jdk.Java, builder, cancelToken, redirectStandardInput, outputHandler, errorHandler)
	{
	}
}
