#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AndroidSdk.Tool;

// The owner stays in this private containment until cleanup completes. Native launchers
// can exit before their children; ancestry-based Process.Kill cannot find those orphans.
internal sealed class AvdProcessContainment : IDisposable
{
	readonly int owner = Environment.ProcessId;
	readonly SafeFileHandle? job;

	public AvdProcessContainment()
	{
		if (OperatingSystem.IsWindows())
		{
			job = CreateJobObject(IntPtr.Zero, null);
			if (job.IsInvalid)
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the private AVD process job.");
			using var current = Process.GetCurrentProcess();
			if (!AssignProcessToJobObject(job, current.Handle))
			{
				var error = Marshal.GetLastWin32Error();
				job.Dispose();
				throw new Win32Exception(error, "Could not assign the AVD owner to its private process job.");
			}
		}
		else if (setsid() < 0)
		{
			var error = Marshal.GetLastWin32Error();
			if (getpgrp() != owner)
				throw new Win32Exception(error, "Could not isolate the AVD owner's process group.");
		}
	}

	bool Owns(int pid)
	{
		if (pid <= 0 || pid == owner)
			return false;
		if (OperatingSystem.IsWindows())
		{
			using var process = Process.GetProcessById(pid);
			if (!IsProcessInJob(process.Handle, job!, out var belongs))
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not verify AVD child job membership.");
			return belongs;
		}
		return getpgid(pid) == owner;
	}

	List<Process> Children()
	{
		var result = new List<Process>();
		try
		{
			if (job == null)
			{
				foreach (var process in Process.GetProcesses())
				{
					if (process.Id > 0 && process.Id != owner && getpgid(process.Id) == owner && !process.HasExited)
						result.Add(process);
					else
						process.Dispose();
				}
			}
			else
			{
				var size = 4096;
				while (true)
				{
					var buffer = Marshal.AllocHGlobal(size);
					try
					{
						if (QueryInformationJobObject(job, 3, buffer, size, out _))
						{
							var count = Marshal.ReadInt32(buffer, 4);
							for (var i = 0; i < count; i++)
							{
								var pid = checked((int)Marshal.ReadIntPtr(buffer, 8 + i * IntPtr.Size).ToInt64());
								if (pid > 0 && pid != owner)
								{
									try { result.Add(Process.GetProcessById(pid)); }
									catch (ArgumentException) { } // Already reaped.
								}
							}
							break;
						}
						var error = Marshal.GetLastWin32Error();
						if (error != 234)
							throw new Win32Exception(error, "Could not enumerate the private AVD process job.");
						size = checked(Math.Max(size * 2, 8 + Marshal.ReadInt32(buffer) * IntPtr.Size));
					}
					finally { Marshal.FreeHGlobal(buffer); }
				}
			}
			return result;
		}
		catch
		{
			foreach (var process in result)
				process.Dispose();
			throw;
		}
	}

	public void StopAndWait()
	{
		var clock = Stopwatch.StartNew();
		while (clock.Elapsed < TimeSpan.FromSeconds(10))
		{
			var children = Children();
			try
			{
				if (children.Count == 0)
					return;
				foreach (var process in children)
				{
					try
					{
						if (!process.HasExited && Owns(process.Id))
							process.Kill();
					}
					catch (ArgumentException) { } // Exited between enumeration and membership check.
					catch (InvalidOperationException) when (process.HasExited) { }
				}
				foreach (var process in children)
				{
					var remaining = TimeSpan.FromSeconds(10) - clock.Elapsed;
					if (remaining <= TimeSpan.Zero || !process.WaitForExit((int)Math.Ceiling(remaining.TotalMilliseconds)))
						throw new TimeoutException("An owned AVD descendant did not terminate within 10 seconds.");
				}
			}
			finally
			{
				foreach (var process in children)
					process.Dispose();
			}
		}
		throw new TimeoutException("The private AVD process containment did not become empty within 10 seconds.");
	}

	public void Dispose() => job?.Dispose();

	[DllImport("libc", SetLastError = true)]
	static extern int setsid();
	[DllImport("libc", SetLastError = true)]
	static extern int getpgrp();
	[DllImport("libc", SetLastError = true)]
	static extern int getpgid(int pid);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	static extern SafeFileHandle CreateJobObject(IntPtr attributes, string? name);
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool QueryInformationJobObject(SafeFileHandle job, int informationClass, IntPtr information, int length, out int returnedLength);
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool IsProcessInJob(IntPtr process, SafeFileHandle job, [MarshalAs(UnmanagedType.Bool)] out bool belongs);
}
