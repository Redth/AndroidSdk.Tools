using System;
using System.Collections.Generic;
using System.IO;

namespace AndroidSdk
{
	public partial class ActivityManager
	{
		/// <summary>
		/// Options for Instrumentation
		/// </summary>
		public class ActivityManagerInstrumentOptions
		{
			//-r: Print raw results(otherwise decode report_key_streamresult). Use with[-e perf true] to generate raw output for performance measurements.
			/// <summary>
			/// Gets or sets a value indicating whether to print raw results (otherwise decode report_key_streamresult). Use with a key/value pair of {"perf",{"true"}} to generate raw output for performance measurements.
			/// </summary>
			/// <value><c>true</c> if print raw results; otherwise, <c>false</c>.</value>
			public bool PrintRawResults { get; set; }

			//-e name value: Set argument name to value.For test runners a common form is {"testrunner_flag", {"value","value"}}.
			/// <summary>
			/// Gets or sets the key/value pairs.  For test runners a common form is testrunner_flag=value,value,etc.
			/// </summary>
			/// <value>The key values.</value>
			public Dictionary<string, List<string>> KeyValues { get; set; }

			//-p file: Write profiling data to file.
			/// <summary>
			/// Gets or sets the file to write profiling data to.
			/// </summary>
			/// <value>The profile to file.</value>
			public FileInfo ProfileToFile { get; set; }

			//-w: Wait for instrumentation to finish before returning.Required for test runners.
			/// <summary>
			/// Gets or sets a value indicating whether to wait for instrumentation to finish before returning.  Required for test runners.
			/// </summary>
			/// <value><c>true</c> if wait; otherwise, <c>false</c>.</value>
			public bool Wait { get; set; }

			//--no-window-animation: Turn off window animations while running.
			/// <summary>
			/// Gets or sets a value indicating whether to turn off window animations while running.
			/// </summary>
			/// <value><c>true</c> if no window animation; otherwise, <c>false</c>.</value>
			public bool NoWindowAnimation { get; set; }

			//--user user_id | current: Specify which user instrumentation runs in; current user if not specified.
			/// <summary>
			/// Gets or sets the User to run instrumentation in.  Default is current user if not specified.
			/// </summary>
			/// <value>The run as user.</value>
			public string RunAsUser { get; set; }
		}
	}
}
