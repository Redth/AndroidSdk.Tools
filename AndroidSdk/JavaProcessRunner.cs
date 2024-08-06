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
}
