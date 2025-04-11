using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AndroidSdk;

public partial class AvdManager
{
	/// <summary>
	/// AVD Info
	/// </summary>
	[DataContract]
	public class Avd(string name, string device, string path, string target, string? basedOn)
	{
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		[DataMember(Name = "name")]
		public string Name { get; set; } = name;

		[DataMember(Name = "device")]
		public string Device { get; set; } = device;

		[DataMember(Name = "path")]
		public string Path { get; set; } = path;

		[DataMember(Name = "target")]
		public string Target { get; set; } = target;

		[DataMember(Name = "basedOn")]
		public string? BasedOn { get; set; } = basedOn;

		[DataMember(Name = "properties")]
		public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

		public override string ToString()
		{
			return $"[Name: {Name}, Device: {Device}, Target: {Target}, Path: {Path}, Based On: {BasedOn ?? string.Empty}]";
		}
	}
}
