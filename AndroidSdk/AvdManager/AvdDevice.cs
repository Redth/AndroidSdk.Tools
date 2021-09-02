using System.Runtime.Serialization;

namespace AndroidSdk
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Device Info
		/// </summary>
		[DataContract]
		public class AvdDevice
		{
			/// <summary>
			/// Gets or sets the Device name.
			/// </summary>
			/// <value>The name.</value>
			[DataMember(Name="name")]
			public string Name { get; set; }

			[DataMember(Name = "id")]
			public string Id { get; set; }

			[DataMember(Name = "numericId")]
			public int? NumericId { get; set; }

			[DataMember(Name = "oem")]
			public string Oem { get; set; }

			public override string ToString()
			{
				return $"{Id} | {NumericId} | {Name} | {Oem}";
			}
		}
	}
}
