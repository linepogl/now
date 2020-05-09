using System;

namespace Now {
	public static class Conversions {
		
		public static System.Windows.Media.Color StringToColor(string input) {
			var s = input.Replace("#", "");
			return System.Windows.Media.Color.FromRgb(
					byte.Parse(s.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
					byte.Parse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
					byte.Parse(s.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
		}

		private const long UnixEpochTicks = 621355968000000000;
		public static DateTime EpochMillisecondsToDateTime(long input) {
			return new DateTime(UnixEpochTicks + input * TimeSpan.TicksPerMillisecond);
		}

		
		public static string Base64UrlEncode(byte[] input) {
			if (input == null) return null;
			return Convert.ToBase64String(input)
				.Replace("=", "")
				.Replace("/", "_")
				.Replace("+", "-");
		}

		public static byte[] Base64UrlDecode(string input) {
			if (input == null) return null;

			input = input
					.PadRight(input.Length + (4 - input.Length % 4) % 4, '=')
					.Replace("_", "/")
					.Replace("-", "+");

			return Convert.FromBase64String(input);
		}
	}
}