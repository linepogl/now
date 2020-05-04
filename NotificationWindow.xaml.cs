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

		public void Update() {
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
			var s = Message.From.TrimStart().TrimStart('"').TrimStart();
			DropCapTextBlock.Text = s == "" ? "?" : s.Substring(0, 1);
			LabelsPanel.Children.Clear();
			foreach (var label in Message.Labels) {
				LabelsPanel.Children.Add(new TextBlock {
					Text = label.Name, Height = 18, FontSize = 12, SnapsToDevicePixels = true,
					Margin = new Thickness(1, 0, 3, 0), Padding = new Thickness(8, 0, 8, 0),
					Background = new SolidColorBrush(label.BgColor),
					Foreground = new SolidColorBrush(label.FgColor),
				});
			}

			// Reactions
			StampReadTextBlock.Visibility = this.ReactionLevel == Reaction.MarkAsRead ? Visibility.Visible : Visibility.Hidden;
			StampDeletedTextBlock.Visibility = this.ReactionLevel == Reaction.Delete ? Visibility.Visible : Visibility.Hidden;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			var screen = SystemParameters.WorkArea;
			this.Top = screen.Height - this.Height - 8;
			this.Left = screen.Width - this.Width - 8;
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
			if (e.Key == Key.Left)
				this.Index--;
			else if (e.Key == Key.Right)
				this.Index++;
		}

		private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (e.Delta < 0) this.AnimateHide();
		}


		public void AnimateShow() {
			if (this.IsVisible) return;
			this.Opacity = 0;
			this.Top = SystemParameters.WorkArea.Height;
			this.Show();
			BeginAnimation(OpacityProperty, new DoubleAnimation {
				From = 0.0,
				To = 1.0,
				Duration = TimeSpan.FromMilliseconds(500),
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
			}, HandoffBehavior.SnapshotAndReplace);
			BeginAnimation(TopProperty, new DoubleAnimation {
				From = SystemParameters.WorkArea.Height,
				To = SystemParameters.WorkArea.Height - this.Height - 8,
				Duration = TimeSpan.FromMilliseconds(500),
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			}, HandoffBehavior.SnapshotAndReplace);
		}
		private bool is_hiding = false;
		public void AnimateHide() {
			if (!this.IsVisible) return;
			if (this.is_hiding) return;
			this.is_hiding = true;
			var anim = new DoubleAnimation {
				From = this.Opacity,
				To = 0,
				Duration = TimeSpan.FromMilliseconds(500),
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			};
			anim.Completed += (o, e) => { this.Hide(); this.is_hiding = false; };

			BeginAnimation(TopProperty, new DoubleAnimation {
				From = this.Top,
				To = SystemParameters.WorkArea.Height,
				Duration = TimeSpan.FromMilliseconds(500),
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			}, HandoffBehavior.SnapshotAndReplace);
			BeginAnimation(OpacityProperty, anim, HandoffBehavior.SnapshotAndReplace);
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
			m.Marked = true;
			this.Messages.Remove(m);
			this.Index = this.Index; // try to display another message
			await Gmail.MarkAsRead(m);
		}

		public async Task Delete() {
			var m = this.Message;
			m.Marked = true;
			this.Messages.Remove(m);
			this.Index = this.Index; // try to display another message
			await Gmail.Delete(m);
		}

		enum Reaction { None, MarkAsRead, Delete }
		private Reaction ReactionLevel = Reaction.None;
		private CancellationTokenSource reaction_in_progress = null;
		private async Task React(Reaction reaction) {
			if (reaction_in_progress != null) reaction_in_progress.Cancel();
			this.ReactionLevel = reaction;
			StampReadTextBlock.Visibility = reaction == Reaction.MarkAsRead ? Visibility.Visible : Visibility.Hidden;
			StampDeletedTextBlock.Visibility = reaction == Reaction.Delete ? Visibility.Visible : Visibility.Hidden;
			if (reaction == Reaction.None) return;
			reaction_in_progress = new CancellationTokenSource();
			var token = reaction_in_progress.Token;
			try {
				await Task.Delay(TimeSpan.FromSeconds(1.0), token);
				switch (reaction) {
					case Reaction.MarkAsRead: await this.MarkAsRead(); StampReadTextBlock.Visibility = Visibility.Hidden; break;
					case Reaction.Delete: await this.Delete(); StampDeletedTextBlock.Visibility = Visibility.Hidden; break;
				}
			}
			catch (TaskCanceledException) { }
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

		private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (Mouse.Captured == null)
			{
				this.CaptureMouse();
			}
		}

		private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (this.IsMouseCaptured) {
				this.ReleaseMouseCapture();
				if (this.IsMouseOver) {
					Gmail.Open(this.Message);
				}
			}
		}
	}
}
