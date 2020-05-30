using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Now {
	public partial class EventControl : UserControl {
		private LocalEvent _local_event;
		public LocalEvent LocalEvent {
			get => _local_event;
			set {
				this._local_event = value;
				this.Update();
			}
		}
		
		public EventControl() {
			InitializeComponent();
			this.Update();
		}

		private static Brush GreyBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0xB2, 0xBD));
		private void Update() {
			if (this.LocalEvent == null) {
				this.Bullet.Fill = GreyBrush;
				this.Bullet.ToolTip = "";
				this.MainText.Text = "";
				this.MainText.ToolTip = "";
				this.WhenText.Text = "";
				this.WhenText.ToolTip = "";
				this.DurationText.Text = "";
				this.AttendeesPanel.Visibility = Visibility.Collapsed;
				this.LocationIcon.Visibility = Visibility.Collapsed;
				this.VideoIcon.Visibility = Visibility.Collapsed;
			}
			else {
				this.Bullet.Fill = new SolidColorBrush(this.LocalEvent.Calendar.BgColor);
				this.Bullet.ToolTip = this.LocalEvent.Calendar.Name;
				this.MainText.Text = this.LocalEvent.Subject;
				this.MainText.ToolTip = this.LocalEvent.Subject;


				if (this.LocalEvent.IsFullDay && this.LocalEvent.DateFrom == DateTime.Today) {
					this.WhenText.Text = "today";
					this.WhenText.ToolTip = null;
				}
				else {
					this.WhenText.Text = Tools.FormatRelativeDateTime(this.LocalEvent.DateFrom);
					this.WhenText.ToolTip = this.LocalEvent.IsFullDay
						? this.LocalEvent.DateFrom.ToString("ddd, d MMM", System.Globalization.CultureInfo.GetCultureInfo("en-US"))
						: this.LocalEvent.DateFrom.ToString("ddd, d MMM, HH:mm", System.Globalization.CultureInfo.GetCultureInfo("en-US"))
						;
				}


				this.DurationText.Text = Tools.FormatDuration(this.LocalEvent.Duration);
				
				this.AttendeesPanel.Visibility = Visibility.Visible;
				this.AttendeesText.Text = this.LocalEvent.Attendees.Count.ToString();
				this.AttendeesPanel.ToolTip = String.Join("\n", this.LocalEvent.Attendees);

				if (this.LocalEvent.Location == null) {
					this.LocationIcon.Visibility = Visibility.Collapsed;
				}
				else {
					this.LocationIcon.Visibility = Visibility.Visible;
					this.LocationIcon.ToolTip = this.LocalEvent.Location;
				}

				if (this.LocalEvent.ConferenceLink == null) {
					this.VideoIcon.Visibility = Visibility.Collapsed;
				}
				else {
					this.VideoIcon.Visibility = Visibility.Visible;
					this.VideoIcon.ToolTip = this.LocalEvent.ConferenceLink;
				}
			}
		}

		private void AttendeesPanel_MouseEnter(object sender, MouseEventArgs e) {
			this.AttendeesPanel.Opacity = 1.0;
		}

		private void AttendeesPanel_MouseLeave(object sender, MouseEventArgs e) {
			this.AttendeesPanel.Opacity = 0.5;
		}

		private void LocationPanel_MouseEnter(object sender, MouseEventArgs e) {
			this.LocationPanel.Opacity = 1.0;
		}

		private void LocationPanel_MouseLeave(object sender, MouseEventArgs e) {
			this.LocationPanel.Opacity = 0.5;
		}

		private void VideoPanel_MouseEnter(object sender, MouseEventArgs e) {
			this.VideoPanel.Opacity = 1.0;
		}

		private void VideoPanel_MouseLeave(object sender, MouseEventArgs e) {
			this.VideoPanel.Opacity = 0.5;
		}
	}
}
