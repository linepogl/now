﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Now {
	public partial class NotificationWindow : Window {
		private readonly Gmail Gmail;
		private readonly WebBrowser WebBrowser = new WebBrowser();

		public NotificationWindow(Gmail gmail) {
			InitializeComponent();
			this.Gmail = gmail;
		}

		private LocalMessageCollection Messages = new LocalMessageCollection();
		public void LoadMessages(LocalMessageCollection messages) {
			var mm = new LocalMessageCollection();
			foreach (var m in messages) if (!m.Marked) mm.Add(m);
			this.Messages = mm;
			this.Index = mm.IndexOf(_message);
		}

		private int _index = -1;
		public int Index {
			get => _index;
			set {
				var total = this.Messages.Count();
				_index = total == 0 ? -1
							 : value < 0 ? 0
							 : value >= total ? total - 1
							 : value;
				var old_message = _message;
				_message = _index < 0 ? null : this.Messages[_index];
				if (old_message != _message) { ReactionLevel = 0; Full = false; }
				this.Update();
			}
		}

		private LocalMessage _message = null;
		public LocalMessage Message {
			get => _message;
			set => this.Index = this.Messages.IndexOf(value);
		}

		private static readonly Brush WhiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
		private static readonly Brush BlueBrush = new SolidColorBrush(Color.FromRgb(59, 175, 218));
		private static readonly Brush[] Brushes = new Brush[] {
			new SolidColorBrush(Color.FromRgb(0xDA, 0x44, 0x53)), //DA4453
			new SolidColorBrush(Color.FromRgb(0xE9, 0x57, 0x3F)), //E9573F
			new SolidColorBrush(Color.FromRgb(0xF6, 0xBB, 0x42)), //F6BB42
			new SolidColorBrush(Color.FromRgb(0x8C, 0xC1, 0x52)), //8CC152
			new SolidColorBrush(Color.FromRgb(0x37, 0xBC, 0x9B)), //37BC9B
			new SolidColorBrush(Color.FromRgb(0x3B, 0xAF, 0xDA)), //3BAFDA
			new SolidColorBrush(Color.FromRgb(0x4A, 0x89, 0xDC)), //4A89DC
			new SolidColorBrush(Color.FromRgb(0x96, 0x7A, 0xDC)), //967ADC
			new SolidColorBrush(Color.FromRgb(0xD7, 0x70, 0xAD)), //D770AD
			new SolidColorBrush(Color.FromRgb(0xAA, 0xB2, 0xBD)), //AAB2BD
		};

		public void Update() {
			if (Message == null) { this.AnimateHide(); return; }
			this.PositionWindow();

			// Update bullets
			var total = this.Messages.Count;
			var max = BulletsPanel.Children.Count;
			for (var i = 0; i < max; i++) {
				Ellipse ellipse = (Ellipse)BulletsPanel.Children[i];
				ellipse.Fill = i == Index ? BlueBrush : WhiteBrush;
				ellipse.Visibility = i < total ? Visibility.Visible : Visibility.Hidden;
			}
			BulletsOverflowTextBlock.Visibility = total > max ? Visibility.Visible : Visibility.Hidden;
			BulletsOverflowTextBlock.Foreground = Index >= max ? BlueBrush : WhiteBrush;
			IndexTextBlock.Text = (Index + 1).ToString() + " / " + total.ToString();
			WebBrowser.Body = Message.Body;

			// Update message
			SubjectTextBlock.Text = Message.Subject;
			SubjectTextBlock.ToolTip = Message.Subject;
			SenderTextBlock.Text = Message.From;
			SenderTextBlock.ToolTip = Message.From;
			SnippetTextBlock.Text = Message.Snippet;
			SnippetTextBlock.Visibility = Message.IsInvitation ? Visibility.Hidden : Visibility.Visible;
			InvitationPanel.Visibility = !Message.IsInvitation ? Visibility.Hidden : Visibility.Visible;
			
			AttachmentsPanel.Visibility = Message.CountAttachments == 0 ? Visibility.Hidden : Visibility.Visible;
			AttachmentsTextBlock.Text = Message.CountAttachments.ToString();

			if (Message.IsInvitation) {
				InvitationDateTimeTextBlock.Text = Message.InvitationDateFrom.Value.ToString(@"dddd, d MMM yyyy \a\t HH:mm", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
				InvitationStartsInTextBlock.Text = Tools.FormatRelativeDateTime(Message.InvitationDateFrom);
				InvitationDurationTextBlock.Text = Tools.FormatDuration(Message.InvitationDuration);
			}

			var s = Message.From.TrimStart().TrimStart('"', '<').TrimStart();
			DropCapTextBlock.Text = (s == "" ? "?" : s.Substring(0, 1)).ToUpper();
			DropCapEllipsis.Fill = Brushes[Math.Abs(Message.From.GetHashCode()) % Brushes.Length];

			LabelsPanel.Children.Clear();
			foreach (var label in Message.Labels) {
				LabelsPanel.Children.Add(new TextBlock {
					Text = label.Name, Height = 18, FontSize = 12, SnapsToDevicePixels = true,
					Margin = new Thickness(1, 0, 3, 0), Padding = new Thickness(8, 0, 8, 0),
					Background = new SolidColorBrush(label.BgColor),
					Foreground = new SolidColorBrush(label.FgColor),
				});
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			e.Cancel = true;
			this.AnimateHide();
		}

		private void Window_KeyDown(object sender, KeyEventArgs e) {
			switch (e.Key) {
				case Key.Left: this.Index--; break;
				case Key.Right: this.Index++; break;
				case Key.Up: this.ReactionUp(); break;
				case Key.Down: this.ReactionDown(); break;
			}
		}

		private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (e.Delta < 0)
				this.ReactionDown();
			else
				this.ReactionUp();
		}

		public void AnimateShow() {
			if (this.IsVisible) return;
			this.PositionWindow();
			this.Opacity = 0;
			this.Top = SystemParameters.WorkArea.Height;
			this.Show();
			this.Animate(TopProperty, SystemParameters.WorkArea.Height - this.Height - 8, 0.5);
			this.Animate(OpacityProperty, 1.0, 0.5);
		}
		private bool is_hiding = false;
		public void AnimateHide() {
			if (!this.IsVisible) return;
			if (this.is_hiding) return;
			WebBrowser.Hide();
			this.Full = false;
			this.is_hiding = true;
			this.Animate(TopProperty, SystemParameters.WorkArea.Height, 0.5);
			this.Animate(OpacityProperty, 0.0, 0.5, () => { this.Hide(); this.is_hiding = false; });
		}

		public async Task MarkAsRead() {
			var m = this.Message;
			if (m == null) return;
			m.Marked = true;
			this.Messages.Remove(m);
			this.Index = this.Index; // try to display another message
			await Gmail.MarkAsRead(m);
		}

		public async Task Delete() {
			var m = this.Message;
			if (m == null) return;
			m.Marked = true;
			this.Messages.Remove(m);
			this.Index = this.Index; // try to display another message
			await Gmail.Delete(m);
		}

		private const double StandardHeight = 296;
		private const double ExpandedHeight = 600;
		private const double StandardWidth = 500;
		private const double ExpandedWidth = 1000;
		private const double ActionsHeight = 40;
		private double TargetExtraHeight => this.ReactionLevel == Reaction.None ? 0 : ActionsHeight;
		private double TargetWidth => this.Full ? ExpandedWidth : StandardWidth;
		private double TargetHeight => (this.Full ? ExpandedHeight : StandardHeight) + TargetExtraHeight;
		public bool Full = false;

		private void PositionWindow() {
			var screen = SystemParameters.WorkArea;
			this.Height = this.TargetHeight;
			this.Width = this.TargetWidth;
			this.Top = screen.Height - this.Height - 8;
			this.Left = screen.Width - this.Width - 8;
			this.ActionsBlock.Height = this.TargetExtraHeight;
			var webbrowser_top_margin = this.HeaderBlock.ActualHeight + 16;
			if (Message.IsInvitation) webbrowser_top_margin += this.InvitationPanel.ActualHeight + 16;
			if (this.Full) this.WebBrowser.Show(); else this.WebBrowser.Hide();
			this.WebBrowser.Top = this.Top + webbrowser_top_margin;
			this.WebBrowser.Left = this.Left + 112;
			this.WebBrowser.Width = this.Width - 112 - 8;
			this.WebBrowser.Height = this.Height - webbrowser_top_margin - 48;
		}

		private enum Reaction { None = 0, MarkAsRead = 1, Delete = 2 }
		private Reaction ReactionLevel = Reaction.None;
		private CancellationTokenSource reaction_in_progress = null;
		private async Task React(Reaction reaction) {
			if (reaction_in_progress != null) reaction_in_progress.Cancel();
			reaction_in_progress = new CancellationTokenSource();
			var token = reaction_in_progress.Token;

			this.ReactionLevel = reaction;
			var screen = SystemParameters.WorkArea;
			var target_scroll = 0.0;
			if (reaction == Reaction.MarkAsRead) target_scroll = 0 * ActionsHeight;
			if (reaction == Reaction.Delete) target_scroll = -1 * ActionsHeight;

			this.Animate(TopProperty, screen.Height - this.TargetHeight - 8, 0.25);
			if (this.Full)
			WebBrowser.Animate(TopProperty, screen.Height - this.TargetHeight + this.HeaderBlock.Height + 8, 0.25);
			this.Animate(HeightProperty, this.TargetHeight, 0.25);
			this.ActionsBlock.Animate(HeightProperty, this.TargetExtraHeight, 0.25);
			this.ActionsInnerBlock.Animate(MarginProperty, new Thickness(0, target_scroll, 0, 0), 0.25);

			if (reaction != Reaction.None) {
				try {
					await Task.Delay(TimeSpan.FromMilliseconds(1000), token);
					switch (reaction) {
						case Reaction.MarkAsRead: await this.MarkAsRead(); break;
						case Reaction.Delete: await this.Delete(); break;
					}
				}
				catch (TaskCanceledException) { }
			}
		}

		public async void ReactionUp() {
			switch (ReactionLevel) {
				case Reaction.None: await this.React(Reaction.MarkAsRead); break;
				case Reaction.MarkAsRead: await this.React(Reaction.Delete); break;
			}
		}
		public async void ReactionDown() {
			switch (ReactionLevel) {
				case Reaction.None: this.AnimateHide(); break;
				case Reaction.MarkAsRead: await this.React(Reaction.None); break;
				case Reaction.Delete: await this.React(Reaction.MarkAsRead); break;
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e) {
			this.ToggleWebBrowser();
		}

		private void ToggleWebBrowser() {
			this.Full = !this.Full;
			this.PositionWindow();
			if (this.Full) 
				WebBrowser.Show();
			else 
				WebBrowser.Hide();
		}
	}
}