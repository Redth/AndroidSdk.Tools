using System.Collections.Generic;

namespace Android.Tool
{
	public partial class SdkManager
	{
		/// <summary>
		/// Encapsulates results from Android SDK Manager's listing
		/// </summary>
		public class SdkManagerList
		{
			/// <summary>
			/// Gets or sets the available packages to install.
			/// </summary>
			/// <value>The available packages.</value>
			public List<SdkPackage> AvailablePackages { get; set; } = new List<SdkPackage>();

			/// <summary>
			/// Gets or sets the already installed packages.
			/// </summary>
			/// <value>The installed packages.</value>
			public List<InstalledSdkPackage> InstalledPackages { get; set; } = new List<InstalledSdkPackage>();
		}
	}
}
