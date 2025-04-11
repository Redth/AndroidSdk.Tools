using System.Runtime.Serialization;

namespace AndroidSdk;

public partial class AvdManager
{
	/// <summary>
	/// AVD Target Info
	/// </summary>
	[DataContract]
	public class AvdTarget(string id, int? numericId, string name, string type, int? apiLevel = null, int? revision = null)
	{
		/// <summary>
		/// Gets or sets the AVD target identifier.
		/// </summary>
		/// <value>The identifier.</value>
		[DataMember(Name="id")]
		public string Id { get; set; } = id;

		[DataMember(Name = "numericId")]
		public int? NumericId { get; set; } = numericId;

		[DataMember(Name = "name")]
		public string Name { get; set; } = name;

		[DataMember(Name = "type")]
		public string Type { get; set; } = type;

		[DataMember(Name = "apiLevel")]
		public int? ApiLevel { get; set; } = apiLevel;

		[DataMember(Name = "revision")]
		public int? Revision { get; set; } = revision;

		public override string ToString()
		{
			return $"{Id} | {Name} | {Type} | {ApiLevel} | {Revision}";
		}
	}
}
