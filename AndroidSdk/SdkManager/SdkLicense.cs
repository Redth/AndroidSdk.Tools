
using System.Collections.Generic;

namespace AndroidSdk
{
	public class SdkLicense
	{
		public string Id { get; set; }

		public List<string> License { get; set; } = new();

		public bool Accepted { get; set; }
	}
}
