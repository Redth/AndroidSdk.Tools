namespace AndroidSdk
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Device Info
		/// </summary>
		public class AvdDevice
		{
			/// <summary>
			/// Gets or sets the Device name.
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
