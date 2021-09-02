
using System.Diagnostics;
using System.Runtime.Serialization;

namespace AndroidSdk
{
	public partial class SdkManager
	{
		/// <summary>
		/// Android SDK Package Information.
		/// </summary>
		[DebuggerDisplay("{Path}, Version: {Version}")]
		[DataContract]
		public class SdkPackage
		{
			/// <summary>
			/// Gets or sets the SDK Manager path.
			/// </summary>
			/// <value>The path.</value>
			[DataMember(Name = "path")]
			public string Path { get; set; }

			/// <summary>
			/// Gets or sets the package version.
			/// </summary>
			/// <value>The version.</value>
			[DataMember(Name = "version")]
			public string Version { get; set; }

			/// <summary>
			/// Gets or sets the package description.
			/// </summary>
			/// <value>The description.</value>
			[DataMember(Name = "description")]
			public string Description { get; set; }
		}
	}
}
