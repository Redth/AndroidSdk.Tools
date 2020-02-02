using System;
using System.IO;

namespace AndroidSdk
{
	public partial class PackageManager
	{
		/// <summary>
		/// Android Package Information
		/// </summary>
		public class PackageListInfo
		{
			/// <summary>
			/// Gets or sets the install path.
			/// </summary>
			/// <value>The install path.</value>
			public FileInfo InstallPath { get; set; }

			/// <summary>
			/// Gets or sets the installer.
			/// </summary>
			/// <value>The installer.</value>
			public string Installer { get; set; }

			/// <summary>
			/// Gets or sets the name of the package.
			/// </summary>
			/// <value>The name of the package.</value>
			public string PackageName { get; set; }
		}
	}
}
