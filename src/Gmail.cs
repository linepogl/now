using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;

namespace Now {
	public enum Status {
		NotConnected,
		Connecting,
		SynchronisingFirstTime,
		StandBy,
		Synchronising
	}

	public class Gmail {
		public LocalLabelCollection LocalLabels;
		public LocalMessageCollection LocalMessages;
		public Gmail() {
			this.LocalLabels = new LocalLabelCollection();
			this.LocalMessages = new LocalMessageCollection();
		}

		private GmailService Service = null;
		public UsersResource Api => Service?.Users;

		public event Action Connecting;
		public event Action Connected;
		public event Action Synchronising;
		public event Action Synchronised;
		public event Action StatusChanged;
		public event Action<List<LocalMessage>> NewMessagesReceived;
		public event Action FirstSyncCompleted;
		private Status _status = Status.NotConnected;
		public Status Status {
			get => _status;
			private set { if (_status != value) { _status = value; this.StatusChanged?.Invoke(); } }
		}
		public bool IsConnected => this.Service != null;
		public bool HasSynchronisedEver = false;

		//
		//
		// Connection
		//
		//
		private CancellationTokenSource connection_in_progress = null;
		public async Task Connect() {
			if (connection_in_progress != null) connection_in_progress.Cancel();
			this.Status = Status.Connecting; this.Connecting?.Invoke();
			connection_in_progress = new CancellationTokenSource();
			try {
				var credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Now.res.google_oauth_credentials.json")).Secrets,
					new String[] { GmailService.ScopeConstants.MailGoogleCom }, "user", connection_in_progress.Token
					);
				var initialiser = new BaseClientService.Initializer { ApplicationName = "Now", HttpClientInitializer = credentials };
				this.Service = new GmailService(initialiser);
				this.Status = Status.StandBy; this.Connected?.Invoke();
			}
			catch (Exception) {
				this.Status = Status.NotConnected;
			}
		}


		//
		//
		// Synchronisation
		//
		//
		private CancellationTokenSource sync_in_progress = null;
		public async Task Sync(TimeSpan interval) {
			if (sync_in_progress != null) sync_in_progress.Cancel();
			sync_in_progress = new CancellationTokenSource();
			try {
				var token = sync_in_progress.Token;
				while (!token.IsCancellationRequested) {
					this.Status = this.HasSynchronisedEver ? Status.Synchronising : Status.SynchronisingFirstTime; this.Synchronising?.Invoke();
					await this.LocalLabels.Sync(this);
					var messages = this.LocalMessages.Clone();
					var new_messages = await messages.Sync(this);
					this.LocalMessages = messages;
					var is_first_sync = !this.HasSynchronisedEver;
					this.HasSynchronisedEver = true;
					this.Status = Status.StandBy; this.Synchronised?.Invoke();
					if (is_first_sync)
						this.FirstSyncCompleted?.Invoke();
					else if (new_messages.Count > 0)
						this.NewMessagesReceived?.Invoke(new_messages);
					await Task.Delay(interval, token);
				}
			}
			catch (TaskCanceledException) {
				// do nothing	
			}
			catch (Exception ex) {
				Console.WriteLine(ex);
				this.Status = Status.StandBy;
			}
		}


		//
		//
		// Operations
		//
		//
		public int CountUnmarkedMessages() {
			return LocalMessages.Where(x => !x.Marked).Count();
		}

		public async Task MarkAsRead(LocalMessage message) {
			message.Marked = true;
			this.StatusChanged?.Invoke();
			this.Synchronised?.Invoke();
			await this.Api.Messages
				.Modify(new Google.Apis.Gmail.v1.Data.ModifyMessageRequest { RemoveLabelIds = new string[] { "UNREAD" } }, "me", message.id)
				.ExecuteAsync();
		}

		public async Task Delete(LocalMessage message) {
			message.Marked = true;
			this.StatusChanged?.Invoke();
			this.Synchronised?.Invoke();
			await this.Api.Messages.Trash("me", message.id).ExecuteAsync();
		}

		public void Open(LocalMessage message) {
			System.Diagnostics.Process.Start("chrome",
				" --app-id=pjkljhegncpnkpknbcohdijeoejaedia --start-maximised --app=https://mail.google.com/mail/u/0/#inbox/" + message.id
				);
		}
	}
}