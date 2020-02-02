using System;
using System.IO;

namespace AndroidSdk
{
	public partial class ActivityManager
	{
		/// <summary>
		/// Activity Manager Start Options
		/// </summary>
		public class ActivityManagerStartOptions
		{
			//-D: Enable debugging.
			/// <summary>
			/// Gets or sets a value indicating whether to enable debugging.
			/// </summary>
			/// <value><c>true</c> if enable debugging; otherwise, <c>false</c>.</value>
			public bool EnableDebugging { get; set; }

			//-W: Wait for launch to complete.
			/// <summary>
			/// Gets or sets a value indicating whether to wait for the launch to complete.
			/// </summary>
			/// <value><c>true</c> if wait for launch; otherwise, <c>false</c>.</value>
			public bool WaitForLaunch { get; set; }

			//--start-profiler file: Start profiler and send results to file.
			/// <summary>
			/// Gets or sets the File to profile to and starts the profiler.
			/// </summary>
			/// <value>The File to send profiler results to.</value>
			public FileInfo ProfileToFile { get; set; }

			//-P file: Like --start-profiler, but profiling stops when the app goes idle.
			/// <summary>
			/// Gets or sets a value indicating whether profiling stops when the app goes idle.
			/// </summary>
			/// <value><c>true</c> if profile stops when idle; otherwise, <c>false</c>.</value>
			public bool ProfileUntilIdle { get; set; }

			//-R count: Repeat the activity launch count times. Prior to each repeat, the top activity will be finished.
			/// <summary>
			/// Gets or sets how many times to repeat the activity launch.  Prior to each repeat, the top activity will be finished.
			/// </summary>
			/// <value>The repeat launch.</value>
			public int? RepeatLaunch { get; set; }

			//-S: Force stop the target app before starting the activity.
			/// <summary>
			/// Gets or sets a value indicating whether force stop the target app before starting the activity.
			/// </summary>
			/// <value><c>true</c> if force stop target; otherwise, <c>false</c>.</value>
			public bool ForceStopTarget { get; set; }

			//--opengl-trace: Enable tracing of OpenGL functions.
			/// <summary>
			/// Gets or sets a value indicating whether to enable tracing of OpenGL functions.
			/// </summary>
			/// <value><c>true</c> if enable open GLT race; otherwise, <c>false</c>.</value>
			public bool EnableOpenGLTrace { get; set; }

			//--user user_id | current: Specify which user to run as; if not specified, then run as the current user.
			/// <summary>
			/// Gets or sets which user to run as; if not specified, then run as the current user.
			/// </summary>
			/// <value>The run as user identifier.</value>
			public string RunAsUserId { get; set; }
		}
	}
}
