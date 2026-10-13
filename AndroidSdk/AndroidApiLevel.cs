#nullable enable
using System.Globalization;

namespace AndroidSdk
{
	internal static class AndroidApiLevel
	{
		public static bool TryParse(string? value, out int major, out int? minor)
		{
			major = 0;
			minor = null;
			if (string.IsNullOrWhiteSpace(value))
				return false;

			var parts = value.Trim().Split('.');
			if (parts.Length > 2 ||
				!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out major))
				return false;

			if (parts.Length == 2)
			{
				if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minorValue))
					return false;
				minor = minorValue;
			}

			return true;
		}

		public static string Format(int major, int? minor) =>
			major.ToString(CultureInfo.InvariantCulture) +
			(minor.HasValue ? "." + minor.Value.ToString(CultureInfo.InvariantCulture) : "");
	}
}
