using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Now {
	public class LocalCalendar {
		public readonly string id;
		public Color BgColor = Color.FromRgb(240, 240, 240);
		public Color FgColor = Color.FromRgb(51, 51, 51);
		public string Name = "";

		public LocalCalendar(string id) { this.id = id; }
		public void Load(Google.Apis.Calendar.v3.Data.CalendarListEntry remote_calendar) {
			this.Name = remote_calendar.Summary;
			this.BgColor = Tools.StringToColor(remote_calendar.BackgroundColor);
			this.FgColor = Tools.StringToColor(remote_calendar.ForegroundColor);
		}
	}

	public class LocalCalendarCollection : KeyedCollection<string, LocalCalendar> {
		protected override string GetKeyForItem(LocalCalendar item) { return item.id; }
		public async Task<List<LocalCalendar>> Sync(Gmail gmail) {
			var new_calendars = new List<LocalCalendar>();

			var remote_calendars = (await gmail.CalendarApi.CalendarList.List().ExecuteAsync()).Items;
			if (remote_calendars == null) remote_calendars = new List<Google.Apis.Calendar.v3.Data.CalendarListEntry>();
			var remote_calendar_ids = remote_calendars.Select(x => x.Id).Where(x => x != null).ToHashSet();

			// Remove the calendars that don't exist anymore
			var to_remove = this.Where(x => !remote_calendar_ids.Contains(x.id)).ToArray();
			foreach (var local_calendar in to_remove)
				this.Remove(local_calendar);

			// Add the new calendars
			foreach (var remote_calendar in remote_calendars) {
				if (!this.Contains(remote_calendar.Id)) {
					var local_calendar = new LocalCalendar(remote_calendar.Id);
					new_calendars.Add(local_calendar);
					local_calendar.Load(remote_calendar);
				}
			}
			foreach (var local_calendar in new_calendars) this.Add(local_calendar);
			return new_calendars;
		}
	}
}
