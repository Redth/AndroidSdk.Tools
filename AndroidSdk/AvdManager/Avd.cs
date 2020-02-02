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

			public override string ToString()
			{
				return Name;
			}
		}
	}
}
