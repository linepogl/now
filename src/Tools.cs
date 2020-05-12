using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

[assembly: AssemblyTitle("Now")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Now")]
[assembly: AssemblyCopyright("Copyright ©  2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Now {
	public static class Tools {

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

		public static string Base64UrlEncodeUtf8(string input) {
			return Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(input));
		}

		public static string Base64UrlDecodeUtf8(string input) {
			return System.Text.Encoding.UTF8.GetString(Base64UrlDecode(input));
		}

		public static string FormatDuration(TimeSpan? input) {
			if (input == null) return null;
			var d = input.Value;
			return 
				((d.Hours == 0 ? "" : d.Hours == 1 ? "1 hour" : d.Hours + " hours")
				+ (d.Minutes == 0 ? "" : d.Minutes == 1 ? " 1 minute" : " " + d.Minutes + " minutes")
				).Trim();
		}

		public static string FormatRelativeDateTime(DateTime? input) {
			if (input == null) return null;
			var d = input.Value;
			var diff_days = new DateTime(d.Year, d.Month, d.Day).Subtract(DateTime.Today).Days;
			var diff = d.Subtract(DateTime.Now);
			var past = diff.Ticks < 0;
			if (past) { diff = new TimeSpan(-diff.Ticks); diff_days = -diff_days; }

			switch (diff_days) {
				case 0:
					var r = "";
					r += (diff.Hours == 0 ? "" : diff.Hours == 1 ? " 1 hour" : " " + diff.Hours + " hours");
					r += (diff.Minutes == 0 ? "" : diff.Minutes == 1 ? " 1 minute" : " " + diff.Minutes + " minutes");
					if (r == "") return "now";
					return past ? r.TrimStart() + " ago" : "in" + r;
				case 1:
					return past ? "yesterday" : "tomorrow";
				default:
					return past ? diff_days + " days ago" : "in " + diff_days + " days";
			}
		}


		public static AnimationTimeline GetAnimation<T>(this FrameworkElement ui, DependencyProperty property, T target, double seconds) {
			switch (target) {
				case double t: {
						var p = (double)ui.GetValue(property);
						if (Math.Abs(p - t) >= 0.5) {
							var anim = new DoubleAnimation {
								To = t,
								Duration = TimeSpan.FromSeconds(seconds),
								FillBehavior = FillBehavior.Stop,
								EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
							};
							anim.Completed += (s, e) => { ui.SetValue(property, t); };
							Storyboard.SetTargetName(anim, ui.Name);
							Storyboard.SetTargetProperty(anim, new PropertyPath(property));
							return anim;
						}
						break;
					}

				case Thickness t: {
						var p = (Thickness)ui.GetValue(property);
						if (Math.Abs(p.Top - t.Top) >= 0.5
							|| Math.Abs(p.Left - t.Left) >= 0.5
							|| Math.Abs(p.Bottom - t.Bottom) >= 0.5
							|| Math.Abs(p.Right - t.Right) >= 0.5) {
							var anim = new ThicknessAnimation {
								To = t,
								Duration = TimeSpan.FromSeconds(seconds),
								FillBehavior = FillBehavior.Stop,
								EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
							};
							anim.Completed += (s, e) => { ui.SetValue(property, t); };
							Storyboard.SetTargetName(anim, ui.Name);
							Storyboard.SetTargetProperty(anim, new PropertyPath(property));
							return anim;
						}
						break;
					}
			}
			return null;
		}

		public static void Animate<T>(this FrameworkElement ui, DependencyProperty property, T target, double seconds, Action then = null) {
			var anim = ui.GetAnimation(property, target, seconds);
			if (anim == null) return;
			if (then != null) anim.Completed += (s, e) => { then.Invoke(); };
			ui.BeginAnimation(property, anim, HandoffBehavior.SnapshotAndReplace);
		}

		public static IEnumerable<T> DescendantsOrSelf<T>(this T obj, Func<T, IEnumerable<T>> get_children) {
			yield return obj;
			foreach (var x in obj.Descendants(get_children))
				yield return x;
		}

		public static IEnumerable<T> Descendants<T>(this T obj, Func<T, IEnumerable<T>> get_children) {
			var children = get_children(obj);
			if (children == null) 
				yield break;
			foreach (T x in children.SelectMany(x => x.DescendantsOrSelf(get_children)))
				yield return x;
		}

	}
}