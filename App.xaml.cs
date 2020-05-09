using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Now {
	public partial class App : Application {

		public readonly Gmail Gmail = new Gmail();
		private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan FailOverSyncInterval = TimeSpan.FromSeconds(300);

		#region Components
		public System.Windows.Forms.NotifyIcon NotifyIcon;
		public System.Windows.Forms.ContextMenuStrip ContextMenu;
		public System.Windows.Forms.ToolStripItem ContextMenuSync;
		public System.Windows.Forms.ToolStripItem ContextMenuConn;
		public System.Windows.Forms.ToolStripItem ContextMenuExit;
		public System.Windows.Forms.Timer Timer;
		public KeyboardHook KeyboardHook;
		public NotificationWindow NotificationWindow;
		public System.Drawing.Icon icoNotConnected;
		public System.Drawing.Icon icoConnecting;
		public System.Drawing.Icon icoSynchronising;
		public System.Drawing.Icon icoSynchronisingFirstTime;
		public System.Drawing.Icon icoSynchronisingUnread;
		public System.Drawing.Icon icoNoUnreadMessages;
		public System.Drawing.Icon icoUnreadMessages;
		private void InitialiseComponents() {
			NotificationWindow = new NotificationWindow(Gmail);

			var res = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();

			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			icoNotConnected = System.Drawing.Icon.FromHandle(new System.Drawing.Bitmap(assembly.GetManifestResourceStream("Now.res.status_not_connected.png")).GetHicon());
			icoConnecting = System.Drawing.Icon.FromHandle(new System.Drawing.Bitmap(assembly.GetManifestResourceStream("Now.res.status_connecting.png")).GetHicon());
			icoSynchronising = System.Drawing.Icon.FromHandle(new System.Drawing.Bitmap(assembly.GetManifestResourceStream("Now.res.status_synchronising.png")).GetHicon());
			icoSynchronisingFirstTime = System.Drawing.Icon.FromHandle(new System.Drawing.Bitmap(assembly.GetManifestResourceStream("Now.res.status_synchronising_first_time.png")).GetHicon());
			icoSynchronisingUnread = System.Drawing.Icon.FromHandle(new System.Drawing.Bitmap(assembly.GetManifestResourceStream("Now.res.status_synchronising_unread.png")).GetHicon());
			icoNoUnreadMessages = System.Drawing.Icon.FromHandle(new System.Drawing.Bitmap(assembly.GetManifestResourceStream("Now.res.status_no_unread_messages.png")).GetHicon());
			icoUnreadMessages = System.Drawing.Icon.FromHandle(new System.Drawing.Bitmap(assembly.GetManifestResourceStream("Now.res.status_unread_messages.png")).GetHicon());
			
			NotifyIcon = new System.Windows.Forms.NotifyIcon();
			NotifyIcon.Icon = icoNotConnected;
			NotifyIcon.Text = "Not connected";
			NotifyIcon.Visible = true;
			NotifyIcon.ContextMenuStrip = ContextMenu = new System.Windows.Forms.ContextMenuStrip();
			NotifyIcon.DoubleClick += NotifyIcon_DoubleClicked;

			ContextMenuSync = ContextMenu.Items.Add("Synchronise");
			ContextMenuSync.Available = false;
			ContextMenuSync.Font = new System.Drawing.Font(ContextMenuSync.Font, ContextMenuSync.Font.Style | System.Drawing.FontStyle.Bold);
			ContextMenuSync.Click += ContextMenuSync_Clicked;

			ContextMenuConn = ContextMenu.Items.Add("Connect");
			ContextMenuConn.Available = true;
			ContextMenuConn.Font = new System.Drawing.Font(ContextMenuConn.Font, ContextMenuConn.Font.Style | System.Drawing.FontStyle.Bold);
			ContextMenuConn.Click += ContextMenuConn_Clicked;

			ContextMenu.Items.Add("-");
			ContextMenuExit = NotifyIcon.ContextMenuStrip.Items.Add("Exit");
			ContextMenuExit.Click += ContextMenuExit_Clicked;

			Gmail.StatusChanged += Gmail_StatusChanged;
			Gmail.Connected += Gmail_Connected;
			Gmail.Synchronised += Gmail_Synchronised;
			Gmail.FirstSyncCompleted += Gmail_FirstSyncCompleted;
			Gmail.NewMessagesReceived += Gmail_NewMessagesReceived;

			KeyboardHook = new KeyboardHook();
			KeyboardHook.KeyPressed += KeyboardHook_KeyPressed;
			try { KeyboardHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, System.Windows.Forms.Keys.OemQuestion); } catch (Exception) { }
			try { KeyboardHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, System.Windows.Forms.Keys.OemOpenBrackets); } catch (Exception) { }
			try { KeyboardHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, System.Windows.Forms.Keys.OemCloseBrackets); } catch (Exception) { }
			try { KeyboardHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, System.Windows.Forms.Keys.OemSemicolon); } catch (Exception) { }
			try { KeyboardHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, System.Windows.Forms.Keys.OemPeriod); } catch (Exception) { }

			Timer = new System.Windows.Forms.Timer();
			Timer.Interval = (int)FailOverSyncInterval.TotalMilliseconds;
			Timer.Tick += Timer_Tick;

			Microsoft.Win32.SystemEvents.PowerModeChanged += System_PowerModeChanged;
			Microsoft.Win32.SystemEvents.SessionSwitch += System_SessionSwitch;
		}
		#endregion

					private async void Application_Startup(object sender, StartupEventArgs e) {
			this.InitialiseComponents();
			await Gmail.Connect();
		}

		private void Application_Exit(object sender, ExitEventArgs e) {
			NotifyIcon.Visible = false;
			NotifyIcon.Dispose();
		}

		private async void Gmail_Connected() {
			await Gmail.Sync(SyncInterval);
		}

		private void Gmail_StatusChanged() {
			ContextMenuConn.Available = Gmail.Status == Status.NotConnected || Gmail.Status == Status.Connecting;
			ContextMenuSync.Available = Gmail.Status == Status.Synchronising || Gmail.Status == Status.StandBy;
			ContextMenuSync.Enabled = Gmail.Status == Status.StandBy;
			switch (Gmail.Status) {
				case Status.NotConnected: 
					NotifyIcon.Icon = icoNotConnected;
					NotifyIcon.Text = "Not connected";
					break;
				case Status.Connecting:
					NotifyIcon.Icon = icoConnecting;
					NotifyIcon.Text = "Connecting...";
					break;
				case Status.SynchronisingFirstTime: 
					NotifyIcon.Icon = icoSynchronisingFirstTime;
					NotifyIcon.Text = "Synchronising...";
					break;
				case Status.Synchronising:
					NotifyIcon.Icon = Gmail.CountUnmarkedMessages() == 0 ? icoSynchronising : icoSynchronisingUnread;
					NotifyIcon.Text = "Synchronising...";
					break;
				case Status.StandBy:
					var count = Gmail.CountUnmarkedMessages();
					if (count == 0) {
						NotifyIcon.Icon = icoNoUnreadMessages;
						NotifyIcon.Text = "No unread messages";
					}
					else {
						NotifyIcon.Icon = icoUnreadMessages;
						NotifyIcon.Text = count + (count == 1 ? " unread message" : " unread messages");
					}
					break;
			}
		}

		private async void ContextMenuConn_Clicked(object sender, EventArgs e) {
			await Gmail.Connect();
		}

		private void ContextMenuExit_Clicked(object sender, EventArgs e) {
			this.Shutdown();
		}

		private void NotifyIcon_DoubleClicked(object sender, EventArgs e) {
			switch (Gmail.Status) {
				case Status.NotConnected:
				case Status.Connecting:
					ContextMenuConn_Clicked(sender, e);
					break;
				case Status.StandBy:
					ContextMenuSync_Clicked(sender, e);
					break;
			}
		}

		private async void ContextMenuSync_Clicked(object sender, EventArgs e) {
			await Gmail.Sync(SyncInterval);
		}

		private async void Timer_Tick(object sender, EventArgs e) {
			await (Gmail.IsConnected ? Gmail.Sync(SyncInterval) : Gmail.Connect());
		}

		private void System_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e) {
			if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionUnlock) {
				this.Timer.Stop(); this.Timer.Start(); this.Timer_Tick(sender, e);
			}
		}

		private void System_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e) {
			if (e.Mode == Microsoft.Win32.PowerModes.Resume) {
				this.Timer.Stop(); this.Timer.Start(); this.Timer_Tick(sender, e);
			}
		}

		private void Gmail_Synchronised() {
			NotificationWindow.LoadMessages(Gmail.LocalMessages);
		}

		private void Gmail_FirstSyncCompleted() {
			if (Gmail.CountUnmarkedMessages() > 0)
				NotificationWindow.AnimateShow();
		}

		private void Gmail_NewMessagesReceived(List<LocalMessage> new_messages) {
			System.Media.SystemSounds.Exclamation.Play();
			NotificationWindow.Message = new_messages.First();
			NotificationWindow.AnimateShow();
		}

		private void KeyboardHook_KeyPressed(object sender, KeyPressedEventArgs e) {
			if (NotificationWindow.IsVisible) {
				switch (e.Key) {
					case System.Windows.Forms.Keys.OemQuestion: NotificationWindow.AnimateHide(); break;
					case System.Windows.Forms.Keys.OemSemicolon: NotificationWindow.ReactionUp(); break;
					case System.Windows.Forms.Keys.OemPeriod: NotificationWindow.ReactionDown(); break;
					case System.Windows.Forms.Keys.OemOpenBrackets: NotificationWindow.Index--; break;
					case System.Windows.Forms.Keys.OemCloseBrackets: NotificationWindow.Index++; break;
				}
			}
			else {
				switch (e.Key) {
					case System.Windows.Forms.Keys.OemQuestion: NotifyIcon_DoubleClicked(sender, e); break;
					case System.Windows.Forms.Keys.OemSemicolon: if (Gmail.CountUnmarkedMessages() > 0) NotificationWindow.AnimateShow(); break;
				}
			}
		}
	}
}
