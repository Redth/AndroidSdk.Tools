
using System.Collections.Generic;

namespace AndroidSdk;

public class SdkLicense(string id, List<string>? license = null, bool accepted = false)
{
	public string Id { get; set; } = id;

	public List<string> License { get; set; } = license ?? new List<string>();

	public bool Accepted { get; set; } = accepted;
}
