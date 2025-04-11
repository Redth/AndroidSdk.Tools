using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AndroidSdk;

internal class JavaProcessArgumentBuilder : ProcessArgumentBuilder
{
	private static readonly string ClassPathSeparator =
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";

	public JavaProcessArgumentBuilder(JavaProcessArgumentBuilder other)
		: base(other)
	{
		Package = other.Package;
	}

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
		if (!string.IsNullOrWhiteSpace(oldclasspath))
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
		if (!string.IsNullOrWhiteSpace(oldOptions))
			newOptions = string.Join(" ", oldOptions, newOptions);

		SetEnvVar("JAVA_TOOL_OPTIONS", newOptions);
	}

	public override string ToString() =>
		Package + " " + base.ToString();
}
