namespace Android.Tools
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Target Info
		/// </summary>
		public class AvdTarget
		{
			/// <summary>
			/// Gets or sets the AVD target identifier.
			/// </summary>
			/// <value>The identifier.</value>
			public string Id { get; set; }

			public override string ToString()
			{
				return Id;
			}
		}
	}
}
