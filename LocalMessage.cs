using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Now {
	public class LocalMessage {
		public readonly string id;
		public string Subject = "";
		public string Snippet = "";
		public string Body = "";
		public int CountAttachments = 0;
		public DateTime ReceivedAt = DateTime.MinValue;
		public string From = "";
		public string To = "";
		public string CC = "";
		public bool Marked = false;
		public readonly List<LocalLabel> Labels = new List<LocalLabel>();
		public DateTime? InvitationDateFrom = null;
		public DateTime? InvitationDateTill = null;
		public bool IsInvitation => this.InvitationDateFrom != null;

		public LocalMessage(string id) { this.id = id; }
		public async Task Load(Gmail gmail) {
			var remote_message = await gmail.Api.Messages.Get("me", this.id).ExecuteAsync();
			this.ReceivedAt = Conversions.EpochMillisecondsToDateTime(remote_message.InternalDate.GetValueOrDefault(0));
			this.Labels.Clear();
			this.Labels.AddRange(remote_message.LabelIds.Where(x => x.StartsWith("Label_")).Select(x => gmail.LocalLabels[x]).Where(x => x != null));
			this.Labels.Sort((x, y) => String.Compare(x.Name, y.Name));
			this.Snippet = WebUtility.HtmlDecode(remote_message.Snippet);
			foreach (var header in remote_message.Payload.Headers) {
				switch (header.Name) {
					case "From":
						this.From = header.Value;
						break;
					case "To":
						this.To = header.Value;
						break;
					case "Subject":
						this.Subject = string.IsNullOrEmpty(header.Value) ? "" : header.Value;
						break;
					case "CC":
						this.CC = header.Value;
						break;
				}
			}
			if (remote_message.Payload.Parts != null) {
				if (remote_message.Payload.MimeType == "multipart/mixed") {
					this.CountAttachments = remote_message.Payload.Parts.Where(part => !string.IsNullOrEmpty(part.Filename)).Count();
				}
				var invitation_attachment_id = remote_message.Payload.Parts.Where(x => x?.Filename == "invite.ics").FirstOrDefault()?.Body?.AttachmentId;
				if (invitation_attachment_id != null) {
					var invitation = await gmail.Api.Messages.Attachments.Get("me", this.id, invitation_attachment_id).ExecuteAsync();
					var ics = System.Text.Encoding.UTF8.GetString(Conversions.Base64UrlDecode(invitation.Data));
					var vcalendar = Ical.Net.Calendar.Load(ics);
					var vevent = vcalendar.Events.FirstOrDefault();
					this.InvitationDateFrom = vevent.DtStart.AsSystemLocal;
					this.InvitationDateTill = vevent.DtEnd.AsSystemLocal;
				}
			}
		}
	}

	public class LocalMessageCollection : KeyedCollection<string, LocalMessage> {
		protected override string GetKeyForItem(LocalMessage item) { return item.id; }
		public LocalMessageCollection Clone() {
			var r = new LocalMessageCollection();
			foreach (var x in this) r.Add(x);
			return r;
		}
		public async Task<List<LocalMessage>> Sync(Gmail gmail) {
			var new_messages = new List<LocalMessage>();
			
			var top_unread_messages_query = gmail.Api.Messages.List("me");
			top_unread_messages_query.LabelIds = new string[] { "INBOX", "UNREAD" };
			top_unread_messages_query.MaxResults = 100;
			var remote_messages = (await top_unread_messages_query.ExecuteAsync()).Messages;
			if (remote_messages == null) remote_messages = new List<Google.Apis.Gmail.v1.Data.Message>();
			var remote_message_ids = remote_messages.Select(x => x.Id).Where(x => x != null).ToHashSet();

			// Remove the messages that don't exist anymore
			var to_remove = this.Where(x => !remote_message_ids.Contains(x.id)).ToArray();
			foreach (var local_message in to_remove)
				this.Remove(local_message);

			// Add the new messages
			foreach (var remote_message_id in remote_message_ids) {
				if (!this.Contains(remote_message_id)) {
					var local_message = new LocalMessage(remote_message_id);
					new_messages.Add(local_message);
					await local_message.Load(gmail);
				}
			}
			foreach (var local_message in new_messages) this.Add(local_message);
			return new_messages;
		}
	}
}