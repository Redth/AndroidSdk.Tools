using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AndroidSdk;

internal class JavaProcessArgumentBuilder : ProcessArgumentBuilder
{
	private static string ClassPathSeparator =
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";

	public JavaProcessArgumentBuilder(string package)
		: base()
	{
		Package = package;
	}

	public JavaProcessArgumentBuilder(string package, ProcessArgumentBuilder other)
		: base(other)
	{
		Package = package;
	}

	public string Package { get; }

	public void AppendClassPath(params string[] paths) =>
		AppendClassPath((IEnumerable<string>)paths);

	public void AppendClassPath(IEnumerable<string> paths)
	{
		if (!EnvVars.TryGetValue("CLASSPATH", out var oldclasspath) || string.IsNullOrWhiteSpace(oldclasspath))
			oldclasspath = null;

		var newclasspath = string.Join(ClassPathSeparator, paths);
		if (oldclasspath is null)
			newclasspath = string.Join(ClassPathSeparator, oldclasspath, newclasspath);

		SetEnvVar("CLASSPATH", newclasspath);
	}

	public void AppendJavaToolOptions(params string[] options) =>
		AppendJavaToolOptions((IEnumerable<string>)options);

	public void AppendJavaToolOptions(IEnumerable<string> options)
	{
		if (!EnvVars.TryGetValue("JAVA_TOOL_OPTIONS", out var oldOptions) || string.IsNullOrWhiteSpace(oldOptions))
			oldOptions = null;

		var newOptions = string.Join(" ", options);
		if (oldOptions is null)
			newOptions = string.Join(" ", oldOptions, newOptions);

		SetEnvVar("JAVA_TOOL_OPTIONS", newOptions);
	}

	public override string ToString() =>
		Package + " " + base.ToString();
}
