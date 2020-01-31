using System;
using System.IO;

namespace Android.Tool.Adb
{
	/// <summary>
	/// Logcat Options
	/// </summary>
	public class AdbLogcatOptions
	{
		/// <summary>
		/// Gets or sets an alternate log buffer for viewing, such as events or radio.The main buffer is used by default. See Viewing Alternative Log Buffers.
		/// </summary>
		/// <value>The type of the buffer.</value>
		public AdbLogcatBufferType BufferType { get; set; } = AdbLogcatBufferType.Main;

		/// <summary>
		/// Gets or sets a value indicating whether to clears (flush) the entire log and exit.
		/// </summary>
		/// <value><c>true</c> if clear; otherwise, <c>false</c>.</value>
		public bool Clear { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to dump the log and exit.
		/// </summary>
		/// <value><c>true</c> if dump; otherwise, <c>false</c>.</value>
		//public bool Dump { get; set; }

		//-f<filename> Writes log message output to<filename>.The default is stdout.
		/// <summary>
		/// Gets or sets the file to write log message output to.  The default is stdout.
		/// </summary>
		/// <value>The output file.</value>
		public FileInfo OutputFile { get; set; }

		//-g Prints the size of the specified log buffer and exits.
		/// <summary>
		/// Gets or sets a value indicating whether to Print the size of the specified log buffer and exit.
		/// </summary>
		/// <value><c>true</c> if print size; otherwise, <c>false</c>.</value>
		public bool PrintSize { get; set; }

		//-n<count> Sets the maximum number of rotated logs to<count>.The default value is 4. Requires the -r option.
		/// <summary>
		/// Gets or sets the maximum number of rotated logs to.  The default value is 4.  Requires specifying the LogRotationKb option.
		/// </summary>
		/// <value>The number rotated logs.</value>
		public int? NumRotatedLogs { get; set; }

		//-r<kbytes> Rotates the log file every <kbytes> of output. The default value is 16. Requires the -f option.
		/// <summary>
		/// Gets or sets the log rotation size.  The default value is 16.  Requires the OutputFile to be specified.
		/// </summary>
		/// <value>The log rotation kb.</value>
		public int? LogRotationKb { get; set; }

		//-s Sets the default filter spec to silent.
		/// <summary>
		/// Gets or sets a value indicating whether the default filter spec is silent.
		/// </summary>
		/// <value><c>true</c> if silent filter; otherwise, <c>false</c>.</value>
		public bool SilentFilter { get; set; }

		//-v<format> Sets the output format for log messages. The default is brief format. For a list of supported formats, see Controlling Log Output Format.
		/// <summary>
		/// Gets or sets the verbosity.  The default is brief format. For a list of supported formats, see Controlling Log Output Format.
		/// </summary>
		/// <value>The verbosity.</value>
		public AdbLogcatOutputVerbosity Verbosity { get; set; } = AdbLogcatOutputVerbosity.Brief;
	}

	/// <summary>
	/// Which Logcat buffer to return.
	/// </summary>
	public enum AdbLogcatBufferType
	{
		/// <summary>
		/// Main - Main buffer.
		/// </summary>
		Main = 0,
		/// <summary>
		/// Radio - View the buffer that contains radio/telephony related messages.
		/// </summary>
		Radio = 1,
		/// <summary>
		/// Events - View the buffer containing events-related messages.
		/// </summary>
		Events = 2
	}

	/// <summary>
	/// Verbosity of Logcat output to return.
	/// </summary>
	public enum AdbLogcatOutputVerbosity
	{
		/// <summary>
		/// Brief - Display priority/tag and PID of the process issuing the message (the default format).
		/// </summary>
		Brief = 0,
		/// <summary>
		/// Process - Display PID only.
		/// </summary>
		Process,
		/// <summary>
		/// Tag - Display the priority/tag only.
		/// </summary>
		Tag,
		/// <summary>
		/// Raw - Display the raw log message, with no other metadata fields.
		/// </summary>
		Raw,
		/// <summary>
		/// Time - Display the date, invocation time, priority/tag, and PID of the process issuing the message.
		/// </summary>
		Time,
		/// <summary>
		/// ThreadTime - Display the date, invocation time, priority, tag, and the PID and TID of the thread issuing the message.
		/// </summary>
		ThreadTime,
		/// <summary>
		/// Long - Display all metadata fields and separate messages with blank lines.
		/// </summary>
		Long,
	}
}
