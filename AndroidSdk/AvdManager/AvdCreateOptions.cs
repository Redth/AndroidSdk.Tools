namespace AndroidSdk;

public partial class AvdManager
{
	public class AvdCreateOptions
	{
		public string? Device { get; set; }

		public string? Path { get; set; }

		public string? SdCardPathOrSize { get; set; }

		public bool Force { get; set; } = false;

		public string? Abi { get; set; }

		public string? Skin { get; set; }
	}
}
