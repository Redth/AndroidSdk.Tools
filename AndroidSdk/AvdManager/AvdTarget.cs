using System.Runtime.Serialization;

namespace AndroidSdk
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Target Info
		/// </summary>
		[DataContract]
		public class AvdTarget
		{
			/// <summary>
			/// Gets or sets the AVD target identifier.
			/// </summary>
			/// <value>The identifier.</value>
			[DataMember(Name="id")]
			public string Id { get; set; }

			[DataMember(Name = "numericId")]
			public int? NumericId { get; set; }

			[DataMember(Name = "name")]
			public string Name { get; set; }

			[DataMember(Name = "type")]
			public string Type { get; set; }

			[DataMember(Name = "apiLevel")]
			public int ApiLevel { get; set; }

			[DataMember(Name = "revision")]
			public int Revision { get; set; }

			public override string ToString()
			{
				return $"{Id} | {Name} | {Type} | {ApiLevel} | {Revision}";
			}
		}
	}
}
