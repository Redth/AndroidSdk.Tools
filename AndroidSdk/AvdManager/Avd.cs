using System.Runtime.Serialization;

namespace AndroidSdk
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Info
		/// </summary>
		[DataContract]
		public class Avd
		{
			/// <summary>
			/// Gets or sets the name.
			/// </summary>
			/// <value>The name.</value>
			[DataMember(Name = "name")]
			public string Name { get; set; }

			[DataMember(Name = "device")]
			public string Device { get; set; }

			[DataMember(Name = "path")]
			public string Path { get; set; }

			[DataMember(Name = "target")]
			public string Target { get; set; }

			[DataMember(Name = "basedOn")]
			public string BasedOn { get; set; }

			public override string ToString()
			{
				return $"{Name} | {Device} | {Target} | {Path} | {BasedOn}";
			}
		}
	}
}
