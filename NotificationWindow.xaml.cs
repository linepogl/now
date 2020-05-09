using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Now {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class NotificationWindow : Window {
		private readonly Gmail Gmail;
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
				if (old_message != _message) ReactionLevel = 0;
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
			this.PositionWindow();
			MarkAsReadBlock.Visibility = this.ReactionLevel == Reaction.MarkAsRead ? Visibility.Visible : Visibility.Hidden;
			DeleteBlock.Visibility = this.ReactionLevel == Reaction.Delete ? Visibility.Visible : Visibility.Hidden;
			if (Message == null) { this.AnimateHide(); return; }

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

			// Update message
			SubjectTextBlock.Text = Message.Subject;
			SubjectTextBlock.ToolTip = Message.Subject;
			SenderTextBlock.Text = Message.From;
			SnippetTextBlock.Text = Message.Snippet;
			SnippetTextBlock.Visibility = Message.IsInvitation ? Visibility.Hidden : Visibility.Visible;
			InvitationPanel.Visibility = Message.IsInvitation ? Visibility.Visible : Visibility.Hidden;

			if (Message.IsInvitation) {
				var d = Message.InvitationDateFrom.Value;
				var diff_days = new DateTime(d.Year, d.Month, d.Day).Subtract(DateTime.Today).Days;
				var diff = d.Subtract(DateTime.Now);
				var duration = Message.InvitationDateTill.Value.Subtract(d);

				InvitationDateTimeTextBlock.Text = Message.InvitationDateFrom.Value.ToString(@"dddd, d MMM yyyy \a\t HH:mm", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
				InvitationStartsInTextBlock.Text = 
					(diff_days == 0 ? "in"
					+ (diff.Hours == 0 ? ""
						: diff.Hours == 1 ? " 1 hour"
						: " " + diff.Hours + " hours")
					+ (diff.Minutes == 0 ? ""
						: diff.Minutes == 1 ? " 1 minute"
						: " " + diff.Minutes + " minutes")
					+ (diff.TotalMinutes < 0 ? " the past"
						: diff.TotalMinutes < 1 ? " a bit"
						: "")
					: diff_days == 1 ? "tomorrow"
					: "in " + diff_days + " days"
					);

				InvitationDurationTextBlock.Text = (
						(duration.Hours == 0 ? "" : duration.Hours == 1 ? "1 hour" : duration.Hours + " hours")
					+ (duration.Minutes == 0 ? "" : duration.Minutes == 1 ? " 1 minute" : " " + duration.Minutes + " minutes")
					).Trim();
			}

			var s = Message.From.TrimStart().TrimStart('"', '<').TrimStart();
			DropCapTextBlock.Text = s == "" ? "?" : s.Substring(0, 1);
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

		private void Window_MouseEnter(object sender, MouseEventArgs e) {
			this.Opacity = 1.0D;
		}

		private void Window_MouseLeave(object sender, MouseEventArgs e) {
			this.Opacity = 0.9D;
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			this.AnimateHide();
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
			this.Opacity = 0;
			this.Top = SystemParameters.WorkArea.Height;
			this.Show();
			var anim_opacity = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(500)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
			var anim_top = new DoubleAnimation(SystemParameters.WorkArea.Height - this.Height - 8, TimeSpan.FromMilliseconds(500), FillBehavior.Stop) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
			BeginAnimation(OpacityProperty, anim_opacity, HandoffBehavior.SnapshotAndReplace);
			BeginAnimation(TopProperty, anim_top, HandoffBehavior.SnapshotAndReplace);
		}
		private bool is_hiding = false;
		public void AnimateHide() {
			if (!this.IsVisible) return;
			if (this.is_hiding) return;
			this.is_hiding = true;
			var anim_opacity = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(500)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
			var anim_top = new DoubleAnimation(SystemParameters.WorkArea.Height, TimeSpan.FromMilliseconds(500), FillBehavior.Stop) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
			anim_opacity.Completed += (o, e) => { this.Hide(); this.is_hiding = false; };
			BeginAnimation(TopProperty, anim_top, HandoffBehavior.SnapshotAndReplace);
			BeginAnimation(OpacityProperty, anim_opacity, HandoffBehavior.SnapshotAndReplace);
		}

		private void ButtonMarkAsRead_MouseEnter(object sender, MouseEventArgs e) {
			ButtonMarkAsRead.Background.Opacity = 1.0;
		}

		private void ButtonMarkAsRead_MouseLeave(object sender, MouseEventArgs e) {
			ButtonMarkAsRead.Background.Opacity = 0.5;
		}

		private void ButtonMarkAsRead_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (Mouse.Captured == null) ButtonMarkAsRead.CaptureMouse();
		}

		private async void ButtonMarkAsRead_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (ButtonMarkAsRead.IsMouseCaptured)  {
				ButtonMarkAsRead.ReleaseMouseCapture();
				if (ButtonMarkAsRead.IsMouseOver) {
					await this.MarkAsRead();
				}
			}
		}

		private void ButtonDelete_MouseEnter(object sender, MouseEventArgs e) {
			ButtonDelete.Background.Opacity = 1.0;
		}

		private void ButtonDelete_MouseLeave(object sender, MouseEventArgs e) {
			ButtonDelete.Background.Opacity = 0.5;
		}

		private void ButtonDelete_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (Mouse.Captured == null) ButtonDelete.CaptureMouse();
		}

		private async void ButtonDelete_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (ButtonDelete.IsMouseCaptured) {
				ButtonDelete.ReleaseMouseCapture();
				if (ButtonMarkAsRead.IsMouseOver) {
					await this.MarkAsRead();
				}
			}
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

		private const int StandardHeight = 296;
		private const int ReactionHeight = 48;
		private int TargetHeight => StandardHeight + (int)this.ReactionLevel * ReactionHeight;

		private void PositionWindow() {
			var screen = SystemParameters.WorkArea;
			this.Height = this.TargetHeight;
			this.Top = screen.Height - this.Height - 8;
			this.Left = screen.Width - this.Width - 8;
		}


		private enum Reaction { None = 0, MarkAsRead = 1, Delete = 2 }
		private Reaction ReactionLevel = Reaction.None;
		private CancellationTokenSource reaction_in_progress = null;
		private void React(Reaction reaction) {
			if (reaction_in_progress != null) reaction_in_progress.Cancel();
			reaction_in_progress = new CancellationTokenSource();
			var token = reaction_in_progress.Token;

			var going_in = reaction > this.ReactionLevel;
			this.ReactionLevel = reaction;
			this.MarkAsReadBlock.Visibility = reaction == Reaction.MarkAsRead ? Visibility.Visible : Visibility.Hidden;
			this.DeleteBlock.Visibility = reaction == Reaction.Delete ? Visibility.Visible : Visibility.Hidden;

			var screen = SystemParameters.WorkArea;
			var anim_top = new DoubleAnimation(screen.Height - this.TargetHeight - 8, TimeSpan.FromMilliseconds(250), FillBehavior.Stop);
			var anim_height = new DoubleAnimation(this.TargetHeight, TimeSpan.FromMilliseconds(250), FillBehavior.Stop);
			if (reaction != Reaction.None) {
				anim_height.Completed += async (o, e) => {
					try {
						await Task.Delay(TimeSpan.FromMilliseconds(750), token);
						switch (reaction) {
							default: return;
							case Reaction.MarkAsRead: await this.MarkAsRead(); break;
							case Reaction.Delete: await this.Delete(); break;
						}
					}
					catch (TaskCanceledException) { }
				};
			}
			BeginAnimation(TopProperty, anim_top, HandoffBehavior.SnapshotAndReplace);
			BeginAnimation(HeightProperty, anim_height, HandoffBehavior.SnapshotAndReplace);
		}


		public void ReactionUp() {
			switch (ReactionLevel) {
				case Reaction.None: this.React(Reaction.MarkAsRead); break;
				case Reaction.MarkAsRead: this.React(Reaction.Delete); break;
			}
		}
		public void ReactionDown() {
			switch (ReactionLevel) {
				case Reaction.None: this.AnimateHide(); break;
				case Reaction.MarkAsRead: this.React(Reaction.None); break;
				case Reaction.Delete: this.React(Reaction.MarkAsRead); break;
			}
		}

		private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (Mouse.Captured == null) {
				this.CaptureMouse();
			}
		}

		private async void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (this.IsMouseCaptured) {
				this.ReleaseMouseCapture();
				if (this.IsMouseOver) {
					Gmail.Open(this.Message);
					await this.MarkAsRead();
					this.AnimateHide();
				}
			}
		}
	}
}
