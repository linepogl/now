using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Calendar.v3;

namespace Now {
	public class LocalEvent {
		public readonly string id;
		public string Subject = "";
		public string Snippet = "";
		public string Body = "";
		public string Location = null;
		public int CountAttachments = 0;
		public string Organiser = "";
		public List<string> Attendees = new List<string>();
		public DateTime DateFrom = DateTime.MinValue;
		public DateTime DateTill = DateTime.MinValue;
		public DateTime DateUpdated = DateTime.MinValue;
		public LocalCalendar Calendar = null;
		public bool Tentative = false;
		public TimeSpan Duration => DateTill - DateFrom;
		public string ConferenceLink = null;

		public LocalEvent(string id) { this.id = id; }
		public void Load(Google.Apis.Calendar.v3.Data.Event remote_event) {
			this.DateUpdated = remote_event.Updated.GetValueOrDefault(DateTime.MinValue);
			this.DateFrom = remote_event.Start.DateTime.GetValueOrDefault(DateTime.MinValue);
			this.DateTill = remote_event.End.DateTime.GetValueOrDefault(DateTime.MinValue);
			if (this.DateFrom == DateTime.MinValue) { try { this.DateFrom = DateTime.ParseExact(remote_event.Start.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture); } catch (Exception) { } }
			if (this.DateTill == DateTime.MinValue) { try { this.DateTill = DateTime.ParseExact(remote_event.End.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture); } catch (Exception) { } }
			this.Subject = remote_event.Summary;
			this.Body = remote_event.Description;
			this.Organiser = remote_event.Organizer.DisplayName + " <" + remote_event.Organizer.Email + ">";
			this.Attendees.Add(this.Organiser);
			if (remote_event.Attendees != null) 
				this.Attendees.AddRange(remote_event.Attendees.Where(x => x.Email != remote_event.Organizer.Email).Select(x => x.DisplayName + " <" + x.Email + ">"));
			this.Location = remote_event.Location;
			this.Tentative = remote_event.Status == "tentative";

			
		}
	}

	public class LocalEventCollection : KeyedCollection<string, LocalEvent> {
		protected override string GetKeyForItem(LocalEvent item) { return item.id; }
		public LocalEventCollection Clone() {
			var r = new LocalEventCollection();
			foreach (var x in this) r.Add(x);
			return r;
		}
		public async Task<List<LocalEvent>> Sync(Gmail gmail) {
			var new_events = new List<LocalEvent>();
			var from = DateTime.Now.AddHours(-2);
			var till = DateTime.Today.AddDays(7);

			var remote_events = new List<Google.Apis.Calendar.v3.Data.Event>();
			foreach (var local_calendar in gmail.LocalCalendars) {
				var upcoming_events_query = gmail.CalendarApi.Events.List(local_calendar.id);
				upcoming_events_query.TimeMin = from;
				upcoming_events_query.TimeMax = till;
				upcoming_events_query.MaxResults = 30;
				upcoming_events_query.ShowDeleted = false;
				upcoming_events_query.SingleEvents = true;
				var calendar_remote_events = (await upcoming_events_query.ExecuteAsync()).Items;
				if (calendar_remote_events == null) continue;
				foreach (var calendar_remote_event in calendar_remote_events) 
					calendar_remote_event.ColorId = local_calendar.id; // little hack to keep a link to the current calendar
				remote_events.AddRange(calendar_remote_events);
			}
			
			var remote_event_ids = remote_events.Select(x => x.Id).Where(x => x != null).ToHashSet();
			
			// Remove the events that don't exist anymore
			var to_remove = this.Where(x => !remote_event_ids.Contains(x.id)).ToArray();
			foreach (var local_event in to_remove)
				this.Remove(local_event);

			// Add the new events
			foreach (var remote_event in remote_events) {
				if (!this.Contains(remote_event.Id)) {
					var local_event = new LocalEvent(remote_event.Id);
					local_event.Calendar = gmail.LocalCalendars[remote_event.ColorId];
					new_events.Add(local_event);
					local_event.Load(remote_event);
				}
				else {
					var local_event = this[remote_event.Id];
					if (local_event.DateUpdated < remote_event.Updated.GetValueOrDefault(DateTime.MinValue)) {
						local_event.Load(remote_event);
					}
				}
			}
			foreach (var local_event in new_events) this.Add(local_event);
			this.Items.SortBy(x => x.DateFrom);
			return new_events;
		}
	}
}