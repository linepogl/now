using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using now.Properties;

namespace now {
	public partial class App : Application {

		private void Application_Startup(object sender, StartupEventArgs e) {
			var context_menu = new System.Windows.Forms.ContextMenuStrip();
			var context_menu_test = context_menu.Items.Add("Test");
			context_menu_test.Font = new System.Drawing.Font(context_menu.Font, context_menu.Font.Style | System.Drawing.FontStyle.Bold);
			context_menu_test.Click += ContextMenuTest_Clicked;
			context_menu.Items.Add("-");
			context_menu.Items.Add("E&xit").Click += ContextMenuExit_Clicked;

			var notify_icon = new System.Windows.Forms.NotifyIcon();
			notify_icon.Icon = now.Properties.Resources.ico_starting;
			notify_icon.Visible = true;
			notify_icon.ContextMenuStrip = context_menu;
			notify_icon.DoubleClick += ContextMenuTest_Clicked;
		}

		private void ContextMenuTest_Clicked(object sender, EventArgs e) {
			var w = new MainWindow();
			w.Show();
		}

		private void ContextMenuExit_Clicked(object sender, EventArgs e) {
			this.Shutdown();
		}
	}
}
