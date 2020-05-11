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
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;

namespace Now {
	public class LocalLabel {
		public readonly string id;
		public Color BgColor = Color.FromRgb(240, 240, 240);
		public Color FgColor = Color.FromRgb(51, 51, 51);
		public string Name = "";

		public LocalLabel(string id) { this.id = id; }
		public async Task Load(Gmail gmail) {
			var remote_label = await gmail.Api.Labels.Get("me", this.id).ExecuteAsync();
			if (remote_label.Color != null) {
				this.BgColor = Tools.StringToColor(remote_label.Color.BackgroundColor);
				this.FgColor = Tools.StringToColor(remote_label.Color.TextColor);
			}
			this.Name = remote_label.Name;
		}
	}

	public class LocalLabelCollection : KeyedCollection<string, LocalLabel> {
		protected override string GetKeyForItem(LocalLabel item) { return item.id; }
		public async Task<List<LocalLabel>> Sync(Gmail gmail) {
			var new_labels = new List<LocalLabel>();

			var remote_labels = (await gmail.Api.Labels.List("me").ExecuteAsync()).Labels;
			if (remote_labels == null) remote_labels = new List<Google.Apis.Gmail.v1.Data.Label>();
			var remote_label_ids = remote_labels.Select(x => x.Id).Where(x => x != null).ToHashSet();

			// Remove the labels that don't exist anymore
			var to_remove = this.Where(x => !remote_label_ids.Contains(x.id)).ToArray();
			foreach (var local_label in to_remove)
				this.Remove(local_label);

			// Add the new labels
			foreach (var remote_label_id in remote_label_ids) {
				if (!this.Contains(remote_label_id)) {
					var local_label = new LocalLabel(remote_label_id);
					new_labels.Add(local_label);
					await local_label.Load(gmail);
				}
			}
			foreach (var local_label in new_labels) this.Add(local_label);
			return new_labels;
		}
	}
}
