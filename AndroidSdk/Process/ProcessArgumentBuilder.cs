using System.Collections.Generic;

namespace AndroidSdk;

internal class ProcessArgumentBuilder
{
	private readonly List<string> args;
	private readonly Dictionary<string, string> envvars;
	private string? working;

	public ProcessArgumentBuilder()
	{
		args = new();
		envvars = new();
	}

	public ProcessArgumentBuilder(ProcessArgumentBuilder other)
	{
		args = new(other.args);
		envvars = new(other.envvars);
		working = other.working;
	}

	public IReadOnlyList<string> Args => args;

	public IReadOnlyDictionary<string, string> EnvVars => envvars;

	public string? WorkingDirectory => working;

	public void SetWorkingDirectory(string path) =>
		working = path;

	public void Append(string arg) =>
		args.Add(arg);

	public void Append(string name, string value, string separator = " ") =>
		Append($"{name}{separator}{value}");

	public void AppendQuoted(string arg) =>
		args.Add($"\"{arg}\"");

	public void AppendQuoted(string name, string value, string separator = " ") =>
		Append($"{name}{separator}\"{value}\"");

	public void SetEnvVar(string key, string value) =>
		envvars[key] = value;

	public override string ToString() =>
		string.Join(" ", args);
}
