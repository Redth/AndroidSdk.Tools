
using System.Runtime.Serialization;

namespace AndroidSdk;

public partial class SdkManager
{
	/// <summary>
	/// Installed Android SDK Package information.
	/// </summary>
	[DataContract]
	public class InstalledSdkPackage : SdkPackage
	{
		public InstalledSdkPackage(string path, string version, string location, string? description) : base(path, version, description)
		{
			Location = location;
		}

		/// <summary>
		/// Gets or sets the Installed SDK package location.
		/// </summary>
		/// <value>The location.</value>
		[DataMember(Name = "location")]
		public string Location { get; set; }
	}
}
