using System.Runtime.Serialization;

namespace AndroidSdk;

public partial class AvdManager
{
	/// <summary>
	/// AVD Device Info
	/// </summary>
	[DataContract]
	public class AvdDevice(string id, string name, string? oem = null, int? numericId = null)
	{
		/// <summary>
		/// Gets or sets the Device name.
		/// </summary>
		/// <value>The name.</value>
		[DataMember(Name="name")]
		public string Name { get; set; } = name;

		[DataMember(Name = "id")]
		public string Id { get; set; } = id;

		[DataMember(Name = "numericId")]
		public int? NumericId { get; set; } = numericId;

		[DataMember(Name = "oem")]
		public string? Oem { get; set; } = oem;

		public override string ToString()
		{
			return $"{Id} | {NumericId} | {Name} | {Oem}";
		}
	}
}
