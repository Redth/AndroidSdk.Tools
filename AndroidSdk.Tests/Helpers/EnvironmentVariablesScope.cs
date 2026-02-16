#nullable enable
using System;

namespace AndroidSdk.Tests;

/// <summary>
/// Temporarily sets environment variables and restores previous values on dispose.
/// </summary>
internal sealed class EnvironmentVariablesScope : IDisposable
{
	readonly (string Name, string? Value)[] previousValues;

	public EnvironmentVariablesScope(params (string Name, string? Value)[] values)
	{
		previousValues = new (string Name, string? Value)[values.Length];
		for (var i = 0; i < values.Length; i++)
		{
			var (name, value) = values[i];
			previousValues[i] = (name, Environment.GetEnvironmentVariable(name));
			Environment.SetEnvironmentVariable(name, value);
		}
	}

	public void Dispose()
	{
		foreach (var (name, value) in previousValues)
			Environment.SetEnvironmentVariable(name, value);
	}
}
