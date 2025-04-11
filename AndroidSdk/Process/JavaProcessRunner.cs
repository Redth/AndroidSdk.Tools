using System.IO;
using System.Runtime.InteropServices;
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

	public JavaProcessRunner(DirectoryInfo jdkHome, JavaProcessArgumentBuilder builder)
		: base(JdkInfo.GetJavaPath(jdkHome), builder)
	{
	}

	public JavaProcessRunner(DirectoryInfo jdkHome, JavaProcessArgumentBuilder builder, CancellationToken cancelToken, bool redirectStandardInput = false)
		: base(JdkInfo.GetJavaPath(jdkHome), builder, cancelToken, redirectStandardInput)
	{
	}
}
