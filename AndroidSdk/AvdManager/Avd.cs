namespace AndroidSdk
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Info
		/// </summary>
		public class Avd
		{
			/// <summary>
			/// Gets or sets the name.
			/// </summary>
			/// <value>The name.</value>
			public string Name { get; set; }

			public string Device { get; set; }
			public string Path { get; set; }
			public string Target { get; set; }

			public string BasedOn { get; set; }

			public override string ToString()
			{
				return $"{Name} | {Device} | {Target} | {Path} | {BasedOn}";
			}
		}
	}
}
